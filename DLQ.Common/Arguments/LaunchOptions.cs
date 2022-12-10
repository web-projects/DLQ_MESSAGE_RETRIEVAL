using CommandLine;

namespace DLQ.Common.Arguments
{
    class LaunchOptions
    {
        [Option("logLocation", Required = false, HelpText = "Set location of log directory.")]
        public string LogDirectory { get; set; }

        [Option("logLevels", Required = false, HelpText = "Set log levels.")]
        public int LogLevels { get; set; }
    }
}
