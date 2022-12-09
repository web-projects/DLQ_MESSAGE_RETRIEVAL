using System;

namespace DLQ.MessageProcessor.Messages
{
    [Serializable]
    public class BrokerMessage
    {
        public string StringData { get; set; }
        public CommunicationHeader Header { get; set; }
    }
}
