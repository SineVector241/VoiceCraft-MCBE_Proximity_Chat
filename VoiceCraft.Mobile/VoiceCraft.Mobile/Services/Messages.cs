using System;
using System.Collections.Generic;
using VoiceCraft.Mobile.Models;

namespace VoiceCraft.Mobile.Services
{
    public class ServiceErrorMessage
    {
        public Exception? Exception { get; set; }
    }

    public class MuteUnmuteMessage
    { }

    public class DeafenUndeafen
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
        public List<ParticipantDisplayModel> Participants { get; set; } = new List<ParticipantDisplayModel>();
        public string StatusMessage { get; set; } = "";
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsSpeaking { get; set; }
    }
}
