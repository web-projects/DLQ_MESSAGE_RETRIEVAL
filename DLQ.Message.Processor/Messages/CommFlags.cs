using System;

namespace DLQ.MessageProcessor.Messages
{
    [Serializable]
    public class CommFlags
    {
        public int MessageFlag { get; set; }
        public int AcknowledgementFlag { get; set; }
        public int HeartbeatFlag { get; set; }
        public int TraceFlag { get; set; }
        //CommReserved Internal { get; set; }
        public int SubscriptionFlag { get; set; }
        public string ServiceBusFilter { get; set; }
    }
}
