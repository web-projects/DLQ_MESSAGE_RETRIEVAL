using System;

namespace DLQ.MessageRetrieval.Configuration
{
    [Serializable]
    public class ServiceBus
    {
        public string ManagementConnectionString { get; set; }
        public string ConnectionString { get; set; }
        public string Topic { get; set; }
        public int MaxConcurrentCalls { get; set; } = 3;
        public int SubscriptionMaxDeliveryTime { get; private set; } = 10; // Minutes
        public int SubscriptionAutoDeleteTime { get; private set; } = 30; // Minutes
        public int SubscriptionMessageTTLSec { get; set; } = 30; // Seconds
        public int SubscriptionMessageLockDurationSec { get; private set; } = 30; // Seconds
        public bool DeadLetterOnMessageExpiration { get; set; } = false;
        public string DeadLetterQueuePath { get; set; }
        public string LastFilterNameUsed { get; set; }
        public int DeadLetterQueueCheckSec { get; set; }
    }
}
