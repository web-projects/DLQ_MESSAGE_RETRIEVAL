using DLQ.MessageRetrieval.Configuration.ApplicationConfig;
using DLQ.MessageRetrieval.Configuration.ChannelConfig;
using System;

namespace DLQ.MessageRetrieval.Configuration
{
    [Serializable]
    public class AppConfig
    {
        public Application Application { get; set; }
        public string AllowedHosts { get; set; }
        public Channels Channels { get; set; }
    }
}
