using System;

namespace DLQ.MessageRetrieval.Configuration
{
    [Serializable]
    public class Servers
    {
        public ServiceBus ServiceBus { get; set; }
    }
}
