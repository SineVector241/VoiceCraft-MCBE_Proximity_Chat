using System;
using System.Collections.Generic;

namespace VoiceCraft.Windows.Services
{
    public class ServiceFailedMessage
    {
        public string? Message { get; set; }
    }

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
    { }

    public class UpdateUIMessage
    {
        public List<string> Participants { get; set; } = new List<string>();
        public string StatusMessage { get; set; } = "";
        public bool IsMuted { get; set; }
        public bool IsSpeaking { get; set; }
    }
}
