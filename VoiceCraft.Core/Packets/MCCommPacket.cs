using Newtonsoft.Json;

namespace VoiceCraft.Core.Packets
{
    public abstract class MCCommPacket
    {
        public abstract byte PacketId { get; }
        public string Token { get; set; } = string.Empty;

        public virtual string SerializePacket()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public enum MCCommPacketTypes : byte
    {
        Login,
        Accept,
        Deny,
        Bind,
        Update,
        AckUpdate,
        GetChannels,
        GetChannelSettings,
        SetChannelSettings,
        GetDefaultSettings,
        SetDefaultSettings,

        //Participant Stuff
        DisconnectParticipant,
        SetParticipantBitmask,
        GetParticipantBitmask,
        MuteParticipant,
        UnmuteParticipant,
        DeafenParticipant,
        UndeafenParticipant,

        ChannelMove
    }
}
