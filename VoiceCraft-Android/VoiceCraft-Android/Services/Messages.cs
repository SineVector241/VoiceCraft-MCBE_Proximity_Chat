using System;
using System.Collections.Generic;
using VoiceCraft_Android.Models;

namespace VoiceCraft_Android.Services
{
    public class StartServiceMessage
    {
        public string ServerName { get; set; }
        public SettingsModel Settings { get; set; }
    }

    public class StopServiceMessage
    { }

    public class UpdateUIMessage
    {
        public List<ParticipantModel> Participants { get; set; } = new List<ParticipantModel>();
        public string StatusMessage { get; set; } = "";
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
    }

    public class MuteUnmuteMessage
    {}

    public class DeafenUndeafenMessage
    {}

    public class ServiceErrorMessage
    {
        public Exception Exception { get; set; }
    }

    public class ServiceFailedMessage
    {
        public string Message { get; set; }
    }
}
