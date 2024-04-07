using CommunityToolkit.Mvvm.Messaging.Messages;
using VoiceCraft.Core;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.VoiceCraft;

namespace VoiceCraft.Maui.Services
{
    public class StatusUpdatedMSG : ValueChangedMessage<string>
    {
        public StatusUpdatedMSG(string value) : base(value)
        {
        }
    }

    public class StartedSpeakingMSG : ValueChangedMessage<string?>
    {
        public StartedSpeakingMSG(string? value = null) : base(value)
        {
        }
    }

    public class StoppedSpeakingMSG : ValueChangedMessage<string?>
    {
        public StoppedSpeakingMSG(string? value = null) : base(value)
        {
        }
    }

    public class MutedMSG : ValueChangedMessage<string?>
    {
        public MutedMSG(string? value = null) : base(value)
        {
        }
    }

    public class UnmutedMSG : ValueChangedMessage<string?>
    {
        public UnmutedMSG(string? value = null) : base(value)
        {
        }
    }

    public class DeafenedMSG : ValueChangedMessage<string?>
    {
        public DeafenedMSG(string? value = null) : base(value)
        {
        }
    }

    public class UndeafenedMSG : ValueChangedMessage<string?>
    {
        public UndeafenedMSG(string? value = null) : base(value)
        {
        }
    }

    public class ParticipantJoinedMSG : ValueChangedMessage<VoiceCraftParticipant>
    {
        public ParticipantJoinedMSG(VoiceCraftParticipant value) : base(value)
        {
        }
    }

    public class ParticipantLeftMSG : ValueChangedMessage<VoiceCraftParticipant>
    {
        public ParticipantLeftMSG(VoiceCraftParticipant value) : base(value)
        {
        }
    }

    public class ParticipantUpdatedMSG : ValueChangedMessage<VoiceCraftParticipant>
    {
        public ParticipantUpdatedMSG(VoiceCraftParticipant value) : base(value)
        {
        }
    }

    public class ParticipantStartedSpeakingMSG : ValueChangedMessage<VoiceCraftParticipant>
    {
        public ParticipantStartedSpeakingMSG(VoiceCraftParticipant value) : base(value)
        {
        }
    }

    public class ParticipantStoppedSpeakingMSG : ValueChangedMessage<VoiceCraftParticipant>
    {
        public ParticipantStoppedSpeakingMSG(VoiceCraftParticipant value) : base(value)
        {
        }
    }

    public class ChannelAddedMSG : ValueChangedMessage<Channel>
    {
        public ChannelAddedMSG(Channel value) : base(value)
        {
        }
    }

    public class ChannelRemovedMSG : ValueChangedMessage<Channel>
    {
        public ChannelRemovedMSG(Channel value) : base(value)
        {
        }
    }

    public class ChannelJoinedMSG : ValueChangedMessage<Channel>
    {
        public ChannelJoinedMSG(Channel value) : base(value)
        {
        }
    }

    public class ChannelLeftMSG : ValueChangedMessage<Channel>
    {
        public ChannelLeftMSG(Channel value) : base(value)
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
    public class MuteMSG : ValueChangedMessage<string?>
    {
        public MuteMSG(string? value = null) : base(value)
        {
        }
    }

    public class UnmuteMSG : ValueChangedMessage<string?>
    {
        public UnmuteMSG(string? value = null) : base(value)
        {
        }
    }

    public class DeafenMSG : ValueChangedMessage<string?>
    {
        public DeafenMSG(string? value = null) : base(value)
        {
        }
    }

    public class UndeafenMSG : ValueChangedMessage<string?>
    {
        public UndeafenMSG(string? value = null) : base(value)
        {
        }
    }

    public class JoinChannelMSG : ValueChangedMessage<JoinChannel>
    {
        public JoinChannelMSG(JoinChannel value) : base(value)
        {
        }
    }

    public class LeaveChannelMSG : ValueChangedMessage<string?>
    {
        public LeaveChannelMSG(string? value = null) : base(value)
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
        public Channel Channel { get; set; }
        public string Password { get; set; } = string.Empty;
        public JoinChannel(Channel channel)
        {
            Channel = channel;
        }
    }
}
