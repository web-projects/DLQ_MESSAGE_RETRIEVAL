using CommandLine;

namespace DLQ.Launcher.Arguments
{
    class LaunchOptions
    {
        [Option("solutionDir", Required = false, HelpText = "Set location of solution directory.")]
        public string SolutionDirectory { get; set; }

        [Option("buildConfiguration", Required = false, HelpText = "Set current project build configuration.")]
        public string BuildConfiguration { get; set; }

        [Option("beta", Required = false, HelpText = "Set whether or not we are in beta mode.")]
        public bool Beta { get; set; }
    }
}
