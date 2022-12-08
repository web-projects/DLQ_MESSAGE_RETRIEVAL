using DLQ.Common.Configuration.ApplicationConfig;
using DLQ.Common.Configuration.BackgroundConfig;
using DLQ.Common.Configuration.ChannelConfig;
using System;

namespace DLQ.Common.Configuration
{
    [Serializable]
    public class AppConfig
    {
        public Application Application { get; set; }
        public BackgroundTask BackgroundTask { get; set; }
        public string AllowedHosts { get; set; }
        public Channels Channels { get; set; }
    }
}
