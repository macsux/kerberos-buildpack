using System;
using System.Diagnostics;
using System.Linq;
using CommandDotNet;
using CommandDotNet.Directives;
using CommandDotNet.Execution;

namespace KerberosBuildpack
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var runner = new AppRunner<Commands>()
                .Configure(cfg => cfg.UseMiddleware(async (context, next) =>
                {
                    // make all parameters mandatory
                    var invocation = context.InvocationPipeline.TargetCommand.Invocation;
                    var missingParameters = invocation.Parameters
                        .Select(x => x.Name)
                        .Zip(invocation.ParameterValues, (s, o) => new {Name = s, Value = o})
                        .Where(x => x.Value == null)
                        .ToList();
                    if (missingParameters.Any())
                    {
                        var console = context.Console;
                        var help = context.AppConfig.HelpProvider.GetHelpText(context.InvocationPipeline.TargetCommand.Command);
                        console.Out.WriteLine(help);
                        return 1;
                    }

                    return await next(context);
                    
                }, MiddlewareStages.PostBindValuesPreInvoke ));
            runner.AppSettings.IgnoreUnexpectedOperands = true;
            runner.AppSettings.DefaultArgumentMode = ArgumentMode.Operand;
            return runner.Run(args);
        }
    }
}