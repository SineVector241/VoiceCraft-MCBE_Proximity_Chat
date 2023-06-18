using System;
using System.Collections.Generic;

namespace VoiceCraft.Mobile.Services
{
    public class ServiceErrorMessage
    {
        public Exception? Exception { get; set; }
    }

    public class MuteUnmuteMessage
    { }

    public class StopServiceMessage
    { }

    public class StartServiceMessage
    { }

    public class DisconnectMessage
    {
        public string? Reason { get; set; } = string.Empty;
    }

    public class UpdateUIMessage
    {
        public List<string> Participants { get; set; } = new List<string>();
        public string StatusMessage { get; set; } = "";
        public bool IsMuted { get; set; }
    }
}
