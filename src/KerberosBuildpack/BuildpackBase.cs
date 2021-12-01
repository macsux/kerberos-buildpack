using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

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
        public abstract bool Detect(string buildPath);
        public abstract void Supply(string buildPath, string cachePath, string depsPath, int index);
        public abstract void Finalize(string buildPath, string cachePath, string depsPath, int index);
        public abstract void Release(string buildPath);

        /// <summary>
        /// Code that will execute during the run stage before the app is started
        /// </summary>
        public virtual void PreStartup(string buildPath, string depsPath, int index)
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
        protected abstract void Apply(string buildPath, string cachePath, string depsPath, int index);


        public void PreStartup(int index)
        {
            var appHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            PreStartup(appHome, Environment.GetEnvironmentVariable("DEPS_DIR"), index);
            var profiled = Path.Combine(appHome, ".profile.d");
            InstallStartupEnvVars(profiled, index, true);
        }

        public void Sidecar(int index)
        {
            var appHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Sidecar(appHome, Environment.GetEnvironmentVariable("DEPS_DIR"), index);
        }

        public virtual void Sidecar(string buildPath, string depsPath, int index)
        {
            
        }

        protected void DoApply(string buildPath, string cachePath, string depsPath, int index)
        {
            Apply(buildPath, cachePath, depsPath, index);
            
            var isPreStartOverriden = GetType().GetMethod(nameof(PreStartup), BindingFlags.Instance | BindingFlags.Public, null, new[] {typeof(string),typeof(string),typeof(int) }, null  )?.DeclaringType != typeof(BuildpackBase);
            var buildpackDepsDir = Path.Combine(depsPath, index.ToString());
            Directory.CreateDirectory(buildpackDepsDir);
            var profiled = Path.Combine(buildPath, ".profile.d");
            Directory.CreateDirectory(profiled);
            
            if (isPreStartOverriden) 
            {
                // copy buildpack to deps dir so we can invoke it as part of startup
                foreach(var file in Directory.EnumerateFiles(Path.GetDirectoryName(GetType().Assembly.Location)))
                {
                    File.Copy(file, Path.Combine(buildpackDepsDir, Path.GetFileName(file)), true);
                }

                var prestartCommand = $"{GetType().Assembly.GetName().Name} PreStartup";
                // write startup shell script to call buildpack prestart lifecycle event in deps dir
                var startupScriptName = $"{index:00}_{nameof(KerberosBuildpack)}_startup.sh";
                var startupScript = $"#!/bin/bash\n$DEPS_DIR/{index}/{prestartCommand} {index}\n";
                var sidecarOverriden =  GetType().GetMethod(nameof(Sidecar), BindingFlags.Instance | BindingFlags.Public, null, new[] {typeof(string),typeof(string),typeof(int) }, null  )?.DeclaringType != typeof(BuildpackBase);
                if (sidecarOverriden)
                {
                    startupScript += $"$DEPS_DIR/{index}/{GetType().Assembly.GetName().Name} Sidecar {index} &\n";
                }
                File.WriteAllText(Path.Combine(profiled,startupScriptName), startupScript);
                InstallStartupEnvVars(profiled, index, false);
                GetEnvScriptFile(profiled, index, true); // causes empty env file to be created so it can (potentially) be populated with vars during onstart hook
            }
            
        }

        private string GetEnvScriptFile(string profiled, int index, bool isPreStart)
        {
            var prefix = isPreStart ? "z" : string.Empty;
            var suffix = IsLinux ? ".sh" : ".bat";
            var envScriptName = Path.Combine(profiled, $"{prefix}{index:00}_{nameof(KerberosBuildpack)}_env{suffix}");
            // ensure it's initialized
            if(!File.Exists(envScriptName))
                File.WriteAllText(envScriptName, string.Empty);
            return envScriptName;
        }
        protected void InstallStartupEnvVars(string profiled, int index, bool isPreStart)
        {
            var envScriptName = GetEnvScriptFile(profiled, index, isPreStart);
            
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