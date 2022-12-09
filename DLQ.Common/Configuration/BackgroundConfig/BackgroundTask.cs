using System;

namespace DLQ.Common.Configuration.BackgroundConfig
{
    [Serializable]
    public class BackgroundTask
    {
        public int RefreshTimerSec { get; set; }
    }
}
