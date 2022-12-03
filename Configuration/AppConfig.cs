using System;

namespace DLQ.MessageRetrieval.Configuration
{
    [Serializable]
    public class AppConfig
    {
        public string AllowedHosts { get; set; }
        public Channels Channels { get; set; }
    }
}
