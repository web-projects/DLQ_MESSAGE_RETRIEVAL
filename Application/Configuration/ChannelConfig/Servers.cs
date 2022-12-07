using System;

namespace DLQ.MessageRetrieval.Configuration.ChannelConfig
{
    [Serializable]
    public class Servers
    {
        public ServiceBus ServiceBus { get; set; }
    }
}
