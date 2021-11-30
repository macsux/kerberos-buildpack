using System.Linq;

var argsWithCommand = new[] {"Detect"}.Concat(args).ToArray();
return KerberosBuildpack.Program.Main(argsWithCommand);