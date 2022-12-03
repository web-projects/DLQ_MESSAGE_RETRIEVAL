using System;
using System.Collections.Generic;

namespace DLQ.MessageRetrieval.Configuration
{
    [Serializable]
    public class Channels
    {
        public List<Servers> Servers { get; set; }
    }
}
