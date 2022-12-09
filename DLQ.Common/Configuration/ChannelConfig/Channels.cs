using System;
using System.Collections.Generic;

namespace DLQ.Common.Configuration.ChannelConfig
{
    [Serializable]
    public class Channels
    {
        public List<Servers> Servers { get; set; }
    }
}
