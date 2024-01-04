namespace VoiceCraft.Core.Packets.Interfaces
{
    public interface ISignallingPacket
    {
        public SignallingPacketTypes PacketType { get; set; }
        public IPacketData PacketData { get; set; }
        public byte[] GetPacketStream();
    }
}
