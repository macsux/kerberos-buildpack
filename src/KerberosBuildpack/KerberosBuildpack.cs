using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Kerberos.NET;
using Kerberos.NET.Client;
using Kerberos.NET.Configuration;
using Kerberos.NET.Credentials;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using Kerberos.NET.Transport;
using KerberosCommon;

namespace KerberosBuildpack
{
    public class KerberosBuildpack : SupplyBuildpack 
    {

        protected override void Apply(string buildPath, string cachePath, string depsPath, int index)
        {
            var myDependenciesDirectory = Path.Combine(depsPath, index.ToString()); // store any runtime dependencies not belonging to the app in this directory

            if (!Util.TryGetCredentials(out var credentials))
            {
                return;
            }
            var realm = credentials.Domain.ToUpper();
            var kdcStr = Environment.GetEnvironmentVariable("KRB5_KDC");
            var kdcs = kdcStr != null ? kdcStr.Split(";") : new []{ credentials.Domain };
            
            
            EnvironmentalVariables["KRB5_CONFIG"] = "/home/vcap/app/.krb5/krb5.conf";
            EnvironmentalVariables["KRB5CCNAME"] = "/home/vcap/app/.krb5/krb5cc";
            EnvironmentalVariables["KRB5CCNAME"] = "/home/vcap/app/.krb5/krb5cc";
            EnvironmentalVariables["KRB5_KTNAME"] = "/home/vcap/app/.krb5/service.keytab";
            var krb5Dir = Path.Combine(buildPath, ".krb5");
            var krb5Path = Path.Combine(krb5Dir, "krb5.conf");
            Directory.CreateDirectory(krb5Dir);
            Krb5Config config;
            if (!File.Exists(krb5Path)) // allow user to provide their own krb5 with the app
            {
                config = Krb5Config.Default();
                config.Defaults.DefaultRealm = realm;
                foreach (var kdc in kdcs)
                {
                    config.Realms[realm].Kdc.Add(kdc);
                }
                
                File.WriteAllText(krb5Path, config.Serialize());
            }
            
            // below code attempts to use official way to introduce sidecar via buildpack as described here https://docs.cloudfoundry.org/buildpacks/sidecar-buildpacks.html
            // except it doesn't work and staging never completes - just hands with no error. workaround for now is to have process started as a background executable
            // stuffed into .profile.d startup script, but this makes logs emitted by sidecar show up as if originating from app
            
            
            // var assembly = Assembly.GetExecutingAssembly();
            // var resourceName = assembly.GetManifestResourceNames().Single(x => x.EndsWith("launch.yaml"));

            // using var stream = assembly.GetManifestResourceStream(resourceName);
            // using var reader = new StreamReader(stream);
            // string template = reader.ReadToEnd();
            //
            // var launchYaml = template.Replace("@bpIndex", index.ToString()).Replace("\r","");
            // File.WriteAllText(Path.Combine(myDependenciesDirectory, "launch.yml"), launchYaml);
        }

        // we run the KerberosTicketRefresher twice, first with runonce as a blocking operation to ensure that there's a TGT before any app code starts running
        // it's then started again as a worker process sidecar to do TGT refreshes
        public override void PreStartup(string buildPath, string depsPath, int index)
        {
            if (!Util.TryGetCredentials(out var credentials))
            {
                throw new Exception("Can't generate keytab - credentials not found");
            }

            var krbConfigPath = Environment.GetEnvironmentVariable("KRB5_CONFIG");
            if (krbConfigPath == null || !File.Exists(krbConfigPath))
            {
                throw new Exception("Kerberos config file not set");

            }
            var krbConfig = Krb5Config.Parse(File.ReadAllText(krbConfigPath));
            CreateKeytab(credentials, krbConfig);
            KerberosTicketRefresher.Program.Main(new[]{"--Krb_RunOnce=true"}).Wait();
        }
        
        public override void Sidecar(string buildPath, string depsPath, int index)
        {
            KerberosTicketRefresher.Program.Main(Array.Empty<string>()).Wait();
        }

        private void CreateKeytab(NetworkCredential networkCredential, Krb5Config config)
        {
            var credential = new KerberosPasswordCredential(networkCredential.UserName, networkCredential.Password, networkCredential.Domain);
            credential.Configuration = config;
            LoadSaltFromKdc(credential).Wait();
            var vcapApplication = Environment.GetEnvironmentVariable("VCAP_APPLICATION");
            if (vcapApplication == null)
            {
                throw new InvalidOperationException("VCAP_APPLICATION env var is not set");
            }
            var routes = JsonNode.Parse(vcapApplication)?["application_uris"]?
                .AsArray()
                .Select(x => x.GetValue<string>())
                .ToList() ?? new List<string>();
            if (routes.Count == 0)
            {
                throw new InvalidOperationException("Keytab can't be generate since can't find any routes for the app necessary to figure out SPNs");

            }
            
            var spns = routes.Select(route => $"http/{new Uri($"https://{route}").Host}").ToList();
            var realm = credential.Domain.ToUpper();
            var salt = credential.Salts.First().Value;
            var encryptionType = credential.Salts.First().Key;

            var kerberosKeys = spns.Select(spn => new KerberosKey(networkCredential.Password, new PrincipalName(PrincipalNameType.NT_SRV_HST, realm, new[] { spn }), salt: salt, etype: encryptionType))
                .ToArray();
            var keytabPath = Environment.GetEnvironmentVariable("KRB5_KTNAME") ?? "/home/vcap/app/.krb5/service.keytab";
            var keytab = new KeyTable(kerberosKeys);
            using var fs = new FileStream(keytabPath, FileMode.OpenOrCreate);
            using var bw = new BinaryWriter(fs);
            keytab.Write(bw);
            
            Console.WriteLine($@"Based on the routes assigned to the app, the following SPNs should exist for {networkCredential.UserName}@{networkCredential.Domain}:");
            foreach (var spn in spns)
            {
                Console.WriteLine(spn);
            }
        }
        internal async Task LoadSaltFromKdc(KerberosCredential credential)
        {
            var asReqMessage = KrbAsReq.CreateAsReq(credential, AuthenticationOptions.Renewable);
            var asReq = asReqMessage.EncodeApplication();


            var transport = new KerberosTransportSelector(
                new IKerberosTransport[]
                {
                    new TcpKerberosTransport(null),
                    new UdpKerberosTransport(null),
                    new HttpsKerberosTransport(null)
                },
                credential.Configuration,
                null
            )
            {
                ConnectTimeout = TimeSpan.FromSeconds(5)
            };
            try
            {
                await transport.SendMessage<KrbAsRep>(credential.Domain, asReq);
            }
            catch (KerberosProtocolException pex)
            {
                var paData = pex?.Error?.DecodePreAuthentication();
                if (paData != null)
                {
                    credential.IncludePreAuthenticationHints(paData);
                }
            }
        }
    }
}
