using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceCraft.Client;
using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.Services
{
    public class StatusMessageUpdatedMSG : ValueChangedMessage<string>
    {
        public StatusMessageUpdatedMSG(string value) : base(value)
        {
        }
    }

    public class SpeakingStatusChangedMSG : ValueChangedMessage<bool>
    {
        public SpeakingStatusChangedMSG(bool value) : base(value)
        {
        }
    }

    public class MutedStatusChangedMSG : ValueChangedMessage<bool>
    {
        public MutedStatusChangedMSG(bool value) : base(value)
        {
        }
    }

    public class DeafenedStatusChangedMSG : ValueChangedMessage<bool>
    {
        public DeafenedStatusChangedMSG(bool value) : base(value)
        {
        }
    }

    public class ParticipantAddedMSG : ValueChangedMessage<VoiceCraftParticipant>
    {
        public ParticipantAddedMSG(VoiceCraftParticipant value) : base(value)
        {
        }
    }

    public class ParticipantRemovedMSG : ValueChangedMessage<VoiceCraftParticipant>
    {
        public ParticipantRemovedMSG(VoiceCraftParticipant value) : base(value)
        {
        }
    }

    public class ParticipantChangedMSG : ValueChangedMessage<VoiceCraftParticipant>
    {
        public ParticipantChangedMSG(VoiceCraftParticipant value) : base(value)
        {
        }
    }

    public class ParticipantSpeakingStatusChangedMSG : ValueChangedMessage<ParticipantSpeakingStatusChanged>
    {
        public ParticipantSpeakingStatusChangedMSG(ParticipantSpeakingStatusChanged value) : base(value)
        {
        }
    }

    public class ChannelCreatedMSG : ValueChangedMessage<VoiceCraftChannel>
    {
        public ChannelCreatedMSG(VoiceCraftChannel value) : base(value)
        {
        }
    }

    public class ChannelEnteredMSG : ValueChangedMessage<VoiceCraftChannel>
    {
        public ChannelEnteredMSG(VoiceCraftChannel value) : base(value)
        {
        }
    }

    public class ChannelLeftMSG : ValueChangedMessage<VoiceCraftChannel>
    {
        public ChannelLeftMSG(VoiceCraftChannel value) : base(value)
        {
        }
    }

    public class DisconnectedMSG : ValueChangedMessage<string>
    {
        public DisconnectedMSG(string value) : base(value)
        {
        }
    }

    public class DenyMSG : ValueChangedMessage<string>
    {
        public DenyMSG(string value) : base(value)
        {
        }
    }

    //UI Request Updates
    public class RequestDataMSG : ValueChangedMessage<string?>
    {
        public RequestDataMSG(string? value = null) : base(value)
        {
        }
    }

    public class ResponseDataMSG : ValueChangedMessage<ResponseData>
    {
        public ResponseDataMSG(ResponseData value) : base(value)
        {
        }
    }

    //UI Control Updates
    public class MuteUnmuteMSG : ValueChangedMessage<string?>
    {
        public MuteUnmuteMSG(string? value = null) : base(value)
        {
        }
    }

    public class DeafenUndeafenMSG : ValueChangedMessage<string?>
    {
        public DeafenUndeafenMSG(string? value = null) : base(value)
        {
        }
    }

    public class JoinChannelMSG : ValueChangedMessage<JoinChannel>
    {
        public JoinChannelMSG(JoinChannel value) : base(value)
        {
        }
    }

    public class LeaveChannelMSG : ValueChangedMessage<LeaveChannel>
    {
        public LeaveChannelMSG(LeaveChannel value) : base(value)
        {
        }
    }

    public class DisconnectMSG : ValueChangedMessage<string?>
    {
        public DisconnectMSG(string? value = null) : base(value)
        {
        }
    }

    public class StartServiceMSG : ValueChangedMessage<string?>
    {
        public StartServiceMSG(string? value = null) : base(value)
        {
        }
    }

    public class StopServiceMSG : ValueChangedMessage<string?>
    {
        public StopServiceMSG(string? value = null) : base(value)
        {
        }
    }

    //Message Values
    public class ResponseData
    {
        public List<ParticipantModel> Participants { get; set; }
        public List<ChannelModel> Channels { get; set; }
        public bool IsSpeaking { get; set; }
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public string StatusMessage { get; set; } = string.Empty;

        public ResponseData(List<ParticipantModel> participants, List<ChannelModel> channels, bool isSpeaking, bool isMuted, bool isDeafened, string statusMessage)
        {
            Participants = participants;
            Channels = channels;
            IsSpeaking = isSpeaking;
            IsMuted = isMuted;
            IsDeafened = isDeafened;
            StatusMessage = statusMessage;
        }
    }

    public class JoinChannel
    {
        public VoiceCraftChannel Channel { get; set; }
        public string Password { get; set; } = string.Empty;
        public JoinChannel(VoiceCraftChannel channel)
        {
            Channel = channel;
        }
    }

    public class LeaveChannel
    {
        public VoiceCraftChannel Channel { get; set; }
        public LeaveChannel(VoiceCraftChannel channel)
        {
            Channel = channel;
        }
    }

    public class ParticipantSpeakingStatusChanged
    {
        public VoiceCraftParticipant Participant { get; set; }
        public bool Status { get; set; }

        public ParticipantSpeakingStatusChanged(VoiceCraftParticipant participant, bool status)
        {
            Participant = participant;
            Status = status;
        }
    }
}
