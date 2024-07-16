namespace VoiceCraft.Core.Packets.MCComm
{
    public class ANDModParticipantBitmask : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.ANDModParticipantBitmask;
        public string PlayerId { get; set; } = string.Empty;
        public uint Bitmask { get; set; }
    }
}
