using System;

namespace DLQ.Message.Processor.Messages
{
    [Serializable]
    public class BrokerMessageTest
    {
        public string StringData { get; set; }
        public CommunicationHeader Header { get; set; }
    }
}
