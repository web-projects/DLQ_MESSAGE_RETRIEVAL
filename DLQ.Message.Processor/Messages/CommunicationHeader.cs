using System;

namespace DLQ.Message.Processor.Messages
{
    [Serializable]
    public class CommunicationHeader
    {
        public CommFlags Flags { get; set; }
    }
}
