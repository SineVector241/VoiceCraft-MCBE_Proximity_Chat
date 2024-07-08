namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetSettings : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.GetSettings;
        public byte ChannelId { get; set; }
    }
}
