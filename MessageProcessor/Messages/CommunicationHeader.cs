using System;

namespace DLQ.MessageProcessor.Messages
{
    [Serializable]
    public class CommunicationHeader
    {
        public CommFlags Flags { get; set; }
    }
}
