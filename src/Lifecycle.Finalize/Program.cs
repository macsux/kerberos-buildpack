using System.Linq;

var argsWithCommand = new[] {"Finalize"}.Concat(args).ToArray();
return KerberosBuildpack.Program.Main(argsWithCommand);