namespace VoiceCraft.Core.Packets.MCComm
{
    public class ChannelAdd : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.ChannelAdd;
        public string PlayerId { get; set; } = string.Empty;
        public byte ChannelId { get; set; } = 0; //0 For Main;
    }
}
