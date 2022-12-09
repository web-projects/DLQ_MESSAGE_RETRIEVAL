using System;
using System.Collections.Generic;

namespace DLQ.Common.Configuration.LauncherConfig
{
    [Serializable]
    public class Launcher
    {
        public List<Apps> Apps { get; set; }
    }
}
