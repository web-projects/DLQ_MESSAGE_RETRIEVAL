using System;

namespace DLQ.Common.Configuration.LauncherConfig
{
    [Serializable]
    public class Apps
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int Priority { get; set; } = 1;
        public int LaunchDelaySec { get; set; } = 30;
    }
}
