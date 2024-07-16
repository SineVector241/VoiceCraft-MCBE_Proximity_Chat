namespace VoiceCraft.Core.Packets.MCComm
{
    public class ORModParticipantBitmask : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.ORModParticipantBitmask;
        public string PlayerId { get; set; } = string.Empty;
        public uint Bitmask { get; set; }
    }
}
