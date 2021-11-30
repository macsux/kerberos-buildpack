using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Kerberos.NET.Configuration;
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

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(x => x.EndsWith("launch.yaml"));

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            string template = reader.ReadToEnd();
            
            var launchYaml = template.Replace("@bpIndex", index.ToString());
            File.WriteAllText(Path.Combine(myDependenciesDirectory, "launch.yaml"), launchYaml);
        }
        public override void PreStartup(string buildPath, string depsPath, int index)
        {
            KerberosTicketRefresher.Program.Main(new[]{"--Krb_RunOnce=true"}).Wait();
            EnvironmentalVariables["MY_SETTING"] = "value"; // can set env vars before app starts running
        }

        
    }
}
