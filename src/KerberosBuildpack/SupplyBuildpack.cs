using NMica.Utils.IO;

namespace KerberosBuildpack
{
    public abstract class SupplyBuildpack : BuildpackBase
    {
        public sealed override void Supply(AbsolutePath buildPath, AbsolutePath cachePath, AbsolutePath depsPath, int index)
        {
            DoApply(buildPath, cachePath, depsPath, index);
        }


        // supply buildpacks may get this lifecycle event, but since only one buildpack will be selected if detection is used, it must be final
        // therefore supply buildpacks always must reply with false
        public sealed override bool Detect(AbsolutePath buildPath) => false;
        
        /// <summary>
        /// Only executed on final buildpack, so not applicable to supply buildpacks
        /// </summary>
        public sealed override void Finalize(AbsolutePath buildPath, AbsolutePath cachePath, AbsolutePath depsPath, int index)
        {
            
        }

        /// <summary>
        /// Only executed on final buildpack, so not applicable to supply buildpacks
        /// </summary>
        public override void Release(AbsolutePath buildPath)
        {
            
        }
    }
}