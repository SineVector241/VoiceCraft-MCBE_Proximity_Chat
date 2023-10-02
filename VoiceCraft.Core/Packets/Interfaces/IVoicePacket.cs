namespace VoiceCraft.Core.Packets.Interfaces
{
    public interface IVoicePacket
    {
        public VoicePacketTypes PacketType { get; set; }
        public IPacketData PacketData { get; set; }
        public byte[] GetPacketStream();
    }
}
