using System.Reflection;
using NMica.Utils.IO;

namespace KerberosBuildpack
{
    public class KerberosBuildpack : SupplyBuildpack 
    {

        protected override void Apply(AbsolutePath buildPath, AbsolutePath cachePath, AbsolutePath depsPath, int index)
        {
            Console.WriteLine($"==== Installing Kerberos Buildpack v{ThisAssembly.AssemblyFileVersion} ==== ");
            var myDependenciesDirectory = depsPath / index.ToString(); // store any runtime dependencies not belonging to the app in this directory
            var krb5Dir = buildPath / ".krb5";
            
            EnvironmentalVariables["KRB5_CONFIG"] = "/home/vcap/app/.krb5/krb5.conf";
            EnvironmentalVariables["KRB5CCNAME"] = "/home/vcap/app/.krb5/krb5cc";
            EnvironmentalVariables["KRB5_KTNAME"] = "/home/vcap/app/.krb5/service.keytab";
            EnvironmentalVariables["KRB5_CLIENT_KTNAME"] = "/home/vcap/app/.krb5/service.keytab";
            
            Directory.CreateDirectory(krb5Dir);

            var currentAssemblyDir = ((AbsolutePath)Assembly.GetExecutingAssembly().Location).Parent;
            var buildpackDir = currentAssemblyDir.Parent;
            var sidecarSrcDir = buildpackDir / "deps"  / "sidecar";
            var sidecarTargetDir = myDependenciesDirectory / "sidecar";
            
            FileSystemTasks.CopyDirectoryRecursively(sidecarSrcDir, sidecarTargetDir, DirectoryExistsPolicy.Merge, FileExistsPolicy.Overwrite);
            Console.WriteLine($"Sidecar process copied into $HOME/deps/{index}/sidecar");
            
            var profiled = buildPath / ".profile.d";
            FileSystemTasks.EnsureExistingDirectory(profiled);
            var startSidecarScript = File.ReadAllText(currentAssemblyDir / "startsidecar.sh");
            startSidecarScript = startSidecarScript.Replace("@index", index.ToString());
            var startupScriptName = $"{index:00}_{nameof(KerberosBuildpack)}_startsidecar.sh";
            var startSidecarScriptPath = profiled / startupScriptName;
            File.WriteAllText(startSidecarScriptPath, startSidecarScript);
            Console.WriteLine($"Sidecar process startup script installed into $HOME/app/profile.d/{startupScriptName}");
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

    }
}
