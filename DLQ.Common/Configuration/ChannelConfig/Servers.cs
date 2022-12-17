using System;

namespace DLQ.Common.Configuration.ChannelConfig
{
    [Serializable]
    public class Servers
    {
        public ServiceBusConfiguration ServiceBus { get; set; }
    }
}
