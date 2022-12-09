using System.IO;

namespace DLQ.Launcher.Core
{
    internal static class GlobalArguments
    {
        public static string CurrentDirectory { get; private set; }
        public static string SolutionDirectory { get; private set; }
        public static string SolutionConfiguration { get; private set; }
        public static bool IsBeta { get; private set; }

        static GlobalArguments()
        {
            CurrentDirectory = Directory.GetCurrentDirectory();
        }

        public static void SetSolutionDirectory(string solutionDirectory)
        {
            SolutionDirectory = solutionDirectory;
        }

        public static void SetSolutionConfiguration(string solutionConfiguration)
        {
            SolutionConfiguration = solutionConfiguration;
        }

        public static void SetIsBeta(bool beta)
        {
            IsBeta = beta;
        }
    }

}
