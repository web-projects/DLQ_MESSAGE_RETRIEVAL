using CommandLine;
using DLQ.Launcher.Arguments;
using DLQ.Launcher.Core;

namespace DLQ.Launcher
{
    public static class AppManagerArgumentHelper
    {
        public static void SetApplicationArgumentsIfNecessary(string[] arguments)
        {
#if DEBUG
            Parser.Default.ParseArguments<LaunchOptions>(arguments)
                .WithParsed(e =>
                {
                    if (!string.IsNullOrWhiteSpace(e.SolutionDirectory))
                    {
                        GlobalArguments.SetSolutionDirectory(e.SolutionDirectory);
                    }

                    if (!string.IsNullOrWhiteSpace(e.BuildConfiguration))
                    {
                        GlobalArguments.SetSolutionConfiguration(e.BuildConfiguration);
                    }

                    GlobalArguments.SetIsBeta(e.Beta);
                });
#else
            GlobalArguments.SetSolutionConfiguration("Release");
            GlobalArguments.SetSolutionDirectory(System.IO.Directory.GetCurrentDirectory());
#endif
        }
    }
}
