using System;

namespace DLQ.MessageRetrieval.Messages
{
    [Serializable]
    public class CommunicationHeader
    {
        public CommFlags Flags { get; set; }
    }
}
