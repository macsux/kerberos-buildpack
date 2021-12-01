using CommandDotNet;

namespace KerberosBuildpack
{
    public class Commands
    {
        private readonly KerberosBuildpack _buildpack = new KerberosBuildpack();

        public int Detect([Operand(Description = "Directory path to the application")]string buildPath)
        {
            return _buildpack.Detect(buildPath) ? 0 : 1;
        }

        public void Supply([Operand(Description = "Directory path to the application")]string buildPath, 
            [Operand(Description = "Location the buildpack can use to store assets during the build process")] string cachePath, 
            [Operand(Description = "Directory where dependencies provided by all buildpacks are installed. New dependencies introduced by current buildpack should be stored inside subfolder named with index argument ({depsPath}/{index})")] string depsPath, 
            [Operand(Description = "Number that represents the ordinal position of the buildpack")] int index)
        {
            _buildpack.Supply(buildPath, cachePath, depsPath, index);
        }

        public void Finalize([Operand(Description = "Directory path to the application")]string buildPath, 
            [Operand(Description = "Location the buildpack can use to store assets during the build process")] string cachePath, 
            [Operand(Description = "Directory where dependencies provided by all buildpacks are installed. New dependencies introduced by current buildpack should be stored inside subfolder named with index argument ({depsPath}/{index})")] string depsPath, 
            [Operand(Description = "Number that represents the ordinal position of the buildpack")] int index)
        {
            _buildpack.Finalize(buildPath,cachePath, depsPath, index);
        }

        public void Release([Operand(Description = "Directory path to the application")]string buildPath)
        {
            _buildpack.Release(buildPath);
        }

        public void PreStartup([Operand(Description = "Number that represents the ordinal position of the buildpack")]int index)
        {
            _buildpack.PreStartup(index);
        }
        public void Sidecar([Operand(Description = "Number that represents the ordinal position of the buildpack")]int index)
        {
            _buildpack.Sidecar(index);
        }
    }
}