namespace VoiceCraft.Core.Packets.Interfaces
{
    public interface IMCCommPacket
    {
        public MCCommPacketTypes PacketType { get; set; }
        public IMCCommPacketData PacketData { get; set; }
    }
}
