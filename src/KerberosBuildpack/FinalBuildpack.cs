using System;

namespace KerberosBuildpack
{
    public abstract class FinalBuildpack : BuildpackBase
    {
        public sealed override void Supply(string buildPath, string cachePath, string depsPath, int index)
        {
            // do nothing, we always apply in finalize
        }

        public sealed override void Finalize(string buildPath, string cachePath, string depsPath, int index)
        {
            DoApply(buildPath, cachePath, depsPath, index);
        }

        public sealed override void Release(string buildPath)
        {
            Console.WriteLine("default_process_types:");
            Console.WriteLine($"  web: {GetStartupCommand(buildPath)}");
        }

        /// <summary>
        /// Determines the startup command for the app
        /// </summary>
        /// <param name="buildPath">Directory path to the application</param>
        /// <returns>Startup command executed by Cloud Foundry to launch the application</returns>
        public abstract string GetStartupCommand(string buildPath);
        
    }
}