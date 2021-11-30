using System.Linq;

var argsWithCommand = new[] {"Release"}.Concat(args).ToArray();
return KerberosBuildpack.Program.Main(argsWithCommand);