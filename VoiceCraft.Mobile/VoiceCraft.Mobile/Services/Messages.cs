using System.Collections.Generic;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Mobile.Services
{
    public class StatusMessageUpdatedMSG
    {
        public string Status { get; set; } = string.Empty;
    }

    public class SpeakingStatusChangedMSG
    {
        public bool Status { get; set; }
    }

    public class MutedStatusChangedMSG
    {
        public bool Status { get; set; }
    }

    public class DeafenedStatusChangedMSG
    {
        public bool Status { get; set; }
    }

    public class ParticipantAddedMSG
    {
        public VoiceCraftParticipant Participant { get; set; }
        public ParticipantAddedMSG(VoiceCraftParticipant participant) => Participant = participant;
    }

    public class ParticipantRemovedMSG
    {
        public VoiceCraftParticipant Participant { get; set; }
        public ParticipantRemovedMSG(VoiceCraftParticipant participant) => Participant = participant;
    }

    public class ParticipantChangedMSG
    {
        public VoiceCraftParticipant Participant { get; set; }
        public ParticipantChangedMSG(VoiceCraftParticipant participant) => Participant = participant;
    }

    public class ParticipantSpeakingStatusChangedMSG
    {
        public VoiceCraftParticipant Participant { get; set; }
        public bool Status { get; set; }
        public ParticipantSpeakingStatusChangedMSG(VoiceCraftParticipant participant) => Participant = participant;
    }

    public class ChannelCreatedMSG
    {
        public VoiceCraftChannel Channel { get; set; }
        public ChannelCreatedMSG(VoiceCraftChannel channel) => Channel = channel;
    }

    public class ChannelEnteredMSG
    {
        public VoiceCraftChannel Channel { get; set; }
        public ChannelEnteredMSG(VoiceCraftChannel channel) => Channel = channel;
    }

    public class ChannelLeftMSG
    {
        public VoiceCraftChannel Channel { get; set; }
        public ChannelLeftMSG(VoiceCraftChannel channel) => Channel = channel;
    }

    public class Disconnected
    {
        string Reason { get; set; } = string.Empty;
    }

    //UI Request Updates
    public class RequestData
    { }

    public class ResponseData
    {
        public List<VoiceCraftParticipant> Participants { get; set; } = new List<VoiceCraftParticipant>();
        public bool IsSpeaking { get; set; }
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    //UI Control Updates
    public class MuteUnmuteMSG
    { }

    public class DeafenUndeafenMSG
    { }

    public class DisconnectMSG
    { }
}
