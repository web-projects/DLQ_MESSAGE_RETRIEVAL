using System;
using System.Collections.Generic;

namespace DLQ.MessageRetrieval.Configuration.ChannelConfig
{
    [Serializable]
    public class Channels
    {
        public List<Servers> Servers { get; set; }
    }
}
