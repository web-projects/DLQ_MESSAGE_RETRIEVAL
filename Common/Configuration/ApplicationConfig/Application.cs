using System;

namespace DLQ.Common.Configuration.ApplicationConfig
{
    [Serializable]
    public class Application
    {
        public WindowsPosition WindowsPosition { get; set; }
        public int TotalIterations { get; set; }
        public int NumberofMessagestoSend { get; set; }
    }
}
