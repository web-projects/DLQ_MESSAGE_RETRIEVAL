using CommandLine;

namespace DLQ.Common.Arguments
{
    public class AppArgumentHelper
    {
        public static void SetApplicationArgumentsIfNecessary(string[] arguments)
        {
            Parser.Default.ParseArguments<LaunchOptions>(arguments)
                .WithParsed(e =>
                {
                    if (!string.IsNullOrWhiteSpace(e.LogDirectory))
                    {
                        GlobalArguments.SetLogDirectory(e.LogDirectory);
                    }

                    if (e.LogLevels > 0)
                    {
                        GlobalArguments.SetLogLevels(e.LogLevels);
                    }
                });
        }
    }
}
