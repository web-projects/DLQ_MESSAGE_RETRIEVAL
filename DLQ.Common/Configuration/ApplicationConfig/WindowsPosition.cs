using System;

namespace DLQ.Common.Configuration.ApplicationConfig
{
    [Serializable]
    public class WindowsPosition
    {
        public int Top { get; set; }
        public int Bottom { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
    }
}
