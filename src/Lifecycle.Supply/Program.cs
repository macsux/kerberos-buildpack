using System.Linq;

var argsWithCommand = new[] {"Supply"}.Concat(args).ToArray();
return KerberosBuildpack.Program.Main(argsWithCommand);