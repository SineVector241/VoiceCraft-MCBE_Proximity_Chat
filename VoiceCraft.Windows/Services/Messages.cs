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
    public class RequestUIMessage
    { }

    public class ResponseUIMessage
    {
        public string StatusMessage { get; set; } = string.Empty;
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public List<ParticipantDisplayModel> Participants { get; set; } = new List<ParticipantDisplayModel>();
    }

    public class  UpdateStatusMessage
    {
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class UpdateParticipantsMessage
    {
        public List<ParticipantDisplayModel> Participants { get; set; } = new List<ParticipantDisplayModel>();
    }
}
