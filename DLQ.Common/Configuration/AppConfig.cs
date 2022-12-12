using DLQ.Common.Configuration.ApplicationConfig;
using DLQ.Common.Configuration.BackgroundConfig;
using DLQ.Common.Configuration.ChannelConfig;
using DLQ.Common.Configuration.LauncherConfig;
using DLQ.Common.Configuration.LoggerManagerConfig;
using System;

namespace DLQ.Common.Configuration
{
    [Serializable]
    public class AppConfig
    {
        public string EnvironmentName { get; set; }
        public Application Application { get; set; }
        public BackgroundTask BackgroundTask { get; set; }
        public string AllowedHosts { get; set; }
        public Channels Channels { get; set; }
        public Launcher Launcher { get; set; }
        public LoggingManager LoggingManager { get; set; }
    }
}
