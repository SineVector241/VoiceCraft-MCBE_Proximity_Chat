using System;
using System.Collections.Generic;
using VoiceCraft.Windows.Models;

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

    //Control Messages
    public class MuteUnmuteMessage
    { }

    public class DeafenUndeafen
    { }

    public class StopServiceMessage
    { }

    public class StartServiceMessage
    { }

    public class DisconnectMessage
    { }

    //UI Updates
    public class  UpdateStatusMessage
    {
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class UpdateMessage
    {
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsSpeaking { get; set; }
        public List<ParticipantDisplayModel> Participants { get; set; } = new List<ParticipantDisplayModel>();
    }
}
