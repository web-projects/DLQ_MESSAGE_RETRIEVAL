namespace DLQ.Common.Arguments
{
    public static class GlobalArguments
    {
        public static string LogDirectory { get; private set; }
        public static int LogLevels { get; private set; }

        public static void SetLogDirectory(string logDirectory)
            => LogDirectory = logDirectory;

        public static void SetLogLevels(int logLevels)
            => LogLevels = logLevels;
    }
}
