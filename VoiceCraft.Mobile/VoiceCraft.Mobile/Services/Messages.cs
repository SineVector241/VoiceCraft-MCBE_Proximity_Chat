using System;
using System.Collections.Generic;
using VoiceCraft.Mobile.Models;

namespace VoiceCraft.Mobile.Services
{
    public class ServiceFailedMessage
    {
        public string? Message { get; set; }
    }

    public class ServiceErrorMessage
    {
        public Exception? Exception { get; set; }
    }

    //Control Messages
    public class MuteUnmuteMessage
    {
        public bool Value { get; set; }
    }

    public class DeafenUndeafen
    {
        public bool Value { get; set; }
    }

    public class StopServiceMessage
    { }

    public class StartServiceMessage
    { }

    public class DisconnectMessage
    {
        public string Reason { get; set; } = string.Empty;
    }

    //UI Updates
    public class UpdateStatusMessage
    {
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class UpdateMessage
    {
        public string StatusMessage { get; set; } = string.Empty;
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsSpeaking { get; set; }
        public List<ParticipantDisplayModel> Participants { get; set; } = new List<ParticipantDisplayModel>();
    }
}
