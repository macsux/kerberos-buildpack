using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using NMica.Utils.IO;

namespace KerberosBuildpack
{
    public abstract class BuildpackBase
    {
        /// <summary>
        /// Dictionary of environmental variables to be set at runtime before the app starts
        /// </summary>
        protected Dictionary<string,string> EnvironmentalVariables { get; } = new Dictionary<string, string>();
        protected bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        
        /// <summary>
        /// Determines if the buildpack is compatible and should be applied to the application being staged.
        /// </summary>
        /// <param name="buildPath">Directory path to the application</param>
        /// <returns>True if buildpack should be applied, otherwise false</returns>
        public abstract bool Detect(AbsolutePath buildPath);
        public abstract void Supply(AbsolutePath buildPath, AbsolutePath cachePath, AbsolutePath depsPath, int index);
        public abstract void Finalize(AbsolutePath buildPath, AbsolutePath cachePath, AbsolutePath depsPath, int index);
        public abstract void Release(AbsolutePath buildPath);

        /// <summary>
        /// Code that will execute during the run stage before the app is started
        /// </summary>
        public virtual void PreStartup(AbsolutePath buildPath, AbsolutePath? depsPath, int index)
        {
        }

        /// <summary>
        /// Logic to apply when buildpack is ran.
        /// Note that for <see cref="SupplyBuildpack"/> this will correspond to "bin/supply" lifecycle event, while for <see cref="FinalBuildpack"/> it will be invoked on "bin/finalize"
        /// </summary>
        /// <param name="buildPath">Directory path to the application</param>
        /// <param name="cachePath">Location the buildpack can use to store assets during the build process</param>
        /// <param name="depsPath">Directory where dependencies provided by all buildpacks are installed. New dependencies introduced by current buildpack should be stored inside subfolder named with index argument ({depsPath}/{index})</param>
        /// <param name="index">Number that represents the ordinal position of the buildpack</param>
        protected abstract void Apply(AbsolutePath buildPath, AbsolutePath cachePath, AbsolutePath depsPath, int index);


        public void PreStartup(int index)
        {
            var appHome = (AbsolutePath)Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            PreStartup(appHome, (AbsolutePath)Environment.GetEnvironmentVariable("DEPS_DIR"), index);
            var profiled = appHome / ".profile.d";
            InstallStartupEnvVars(profiled, index, true);
        }

        protected void DoApply(AbsolutePath buildPath, AbsolutePath cachePath, AbsolutePath depsPath, int index)
        {
            
            Apply(buildPath, cachePath, depsPath, index);
            var profiled = buildPath / ".profile.d";
            
            var isPreStartOverriden = GetType().GetMethod(nameof(PreStartup), BindingFlags.Instance | BindingFlags.Public, null, new[] {typeof(AbsolutePath),typeof(AbsolutePath),typeof(int) }, null  )?.DeclaringType != typeof(BuildpackBase);
            var buildpackDepsDir = Path.Combine(depsPath, index.ToString());
            Directory.CreateDirectory(buildpackDepsDir);
            Directory.CreateDirectory(profiled);
            InstallStartupEnvVars(profiled, index, false);
            
            if (isPreStartOverriden) 
            {
                Console.WriteLine("KerberosBuildpack - PreStartup");
                // copy buildpack to deps dir so we can invoke it as part of startup
                var assemblyFolder = Path.GetDirectoryName(GetType().Assembly.Location)!;
                foreach(var file in Directory.EnumerateFiles(assemblyFolder))
                {
                    File.Copy(file, Path.Combine(buildpackDepsDir, Path.GetFileName(file)), true);
                }

                var prestartCommand = $"{GetType().Assembly.GetName().Name} PreStartup";
                // write startup shell script to call buildpack prestart lifecycle event in deps dir
                var startupScriptName = $"{index:00}_{nameof(KerberosBuildpack)}_startup.sh";
                var startupScript = $"#!/bin/bash\n$DEPS_DIR/{index}/{prestartCommand} {index}\n";
                File.WriteAllText(Path.Combine(profiled,startupScriptName), startupScript);
                // InstallStartupEnvVars(profiled, index, false);
                GetEnvScriptFile(profiled, index, true); // causes empty env file to be created so it can (potentially) be populated with vars during onstart hook
            }
            
        }

        private string GetEnvScriptFile(AbsolutePath profiled, int index, bool isPreStart)
        {
            var prefix = isPreStart ? "z" : string.Empty;
            var suffix = IsLinux ? ".sh" : ".bat";
            var envScriptName = Path.Combine(profiled, $"{prefix}{index:00}_{nameof(KerberosBuildpack)}_env{suffix}");
            // ensure it's initialized
            if(!File.Exists(envScriptName))
                File.WriteAllText(envScriptName, "#!/bin/bash\n");
            return envScriptName;
        }
        protected void InstallStartupEnvVars(AbsolutePath profiled, int index, bool isPreStart)
        {
            var envScriptName = GetEnvScriptFile(profiled, index, isPreStart);
            Console.WriteLine(envScriptName);
            if (EnvironmentalVariables.Any())
            {
                if (IsLinux)
                {
                    var envVars = EnvironmentalVariables.Aggregate(new StringBuilder(), (sb,x) => sb.Append($"export {x.Key}={Escape(x.Value)}\n"));
                    File.WriteAllText(envScriptName, $"#!/bin/bash\n{envVars}");
                }
                else
                {
                    var envVars = EnvironmentalVariables.Aggregate(new StringBuilder(), (sb,x) => sb.Append($"SET {x.Key}={x.Value}\r\n"));
                    File.WriteAllText(envScriptName,envVars.ToString());
                }
            }
            
        }

        private static string Escape(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
    }
}