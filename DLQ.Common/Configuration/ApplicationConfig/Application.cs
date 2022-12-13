using System;

namespace DLQ.Common.Configuration.ApplicationConfig
{
    [Serializable]
    public class Application
    {
        public WindowsPosition WindowsPosition { get; set; }
        public int TotalIterations { get; set; }
        public int NumberofMessagestoSend { get; set; }
        public bool RandomizeFilterRule { get; set; }
        public bool RandomizeSubscriptionKey { get; set; }
    }
}
