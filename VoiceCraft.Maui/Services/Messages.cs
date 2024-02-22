using VoiceCraft.Core.Client;
using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.Services
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

    public class DisconnectedMSG
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class DenyMSG
    {
        public string Reason = string.Empty;
    }

    //UI Request Updates
    public class RequestData
    { }

    public class ResponseData
    {
        public List<ParticipantModel> Participants { get; set; } = new List<ParticipantModel>();
        public List<ChannelModel> Channels { get; set; } = new List<ChannelModel>();
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

    public class JoinChannelMSG
    {
        public VoiceCraftChannel Channel { get; set; }
        public string Password { get; set; } = string.Empty;
        public JoinChannelMSG(VoiceCraftChannel channel)
        {
            Channel = channel;
        }
    }

    public class LeaveChannelMSG
    {
        public VoiceCraftChannel Channel { get; set; }
        public LeaveChannelMSG(VoiceCraftChannel channel)
        {
            Channel = channel;
        }
    }

    public class DisconnectMSG
    { }

    public class StartServiceMSG
    { }

    public class StopServiceMSG
    { }
}
