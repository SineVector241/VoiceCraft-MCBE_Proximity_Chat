using Newtonsoft.Json;

namespace VoiceCraft.Core.Packets
{
    public abstract class MCCommPacket
    {
        public abstract byte PacketId { get; }
        public string Token { get; set; } = string.Empty;

        public virtual string SerializePacket(MCCommPacket packet)
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public enum MCCommPacketId : byte
    {
        Login,
        Accept,
        Deny,
        Bind,
        Update,
        UpdateSettings,
        GetSettings,
        RemoveParticipant,
        ChannelMove,
        AckUpdate
    }
}
