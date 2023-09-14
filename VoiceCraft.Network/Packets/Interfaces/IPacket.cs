namespace VoiceCraft.Network.Packets.Interfaces
{
    public interface IPacket
    {
        public SignallingPacketTypes PacketType { get; set; }
        public IPacketData PacketData { get; set; }
        public byte[] GetPacketStream();
    }
}
