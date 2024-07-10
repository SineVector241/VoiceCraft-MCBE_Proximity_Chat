namespace VoiceCraft.Core.Packets.MCComm
{
    public class XORModParticipantBitmask : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.XORModParticipantBitmask;
        public string PlayerId { get; set; } = string.Empty;
        public uint Bitmask { get; set; }
    }
}
