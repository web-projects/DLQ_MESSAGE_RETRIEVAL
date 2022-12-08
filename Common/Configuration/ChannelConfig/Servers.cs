using System;

namespace DLQ.Common.Configuration.ChannelConfig
{
    [Serializable]
    public class Servers
    {
        public ServiceBus ServiceBus { get; set; }
    }
}
