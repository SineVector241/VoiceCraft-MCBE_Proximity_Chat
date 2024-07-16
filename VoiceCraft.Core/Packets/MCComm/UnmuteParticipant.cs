namespace VoiceCraft.Core.Packets.MCComm
{
    public class UnmuteParticipant : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.UnmuteParticipant;
        public string PlayerId { get; set; } = string.Empty;
    }
}
