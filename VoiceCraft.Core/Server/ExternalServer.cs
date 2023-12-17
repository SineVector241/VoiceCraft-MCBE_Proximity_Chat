using System;

namespace VoiceCraft.Core.Server
{
    public class ExternalServer
    {
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public string IP { get; set; } = string.Empty;
    }
}
