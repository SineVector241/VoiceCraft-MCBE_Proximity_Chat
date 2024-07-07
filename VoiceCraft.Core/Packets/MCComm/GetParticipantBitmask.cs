namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetParticipantBitmask : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.GetParticipantBitmask;
        public string PlayerId { get; set; } = string.Empty;
        public ushort Bitmask { get; set; }
    }
}
