namespace VoiceCraft.Core.Packets.MCComm
{
    public class DeafenParticipant : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.DeafenParticipant;
        public string PlayerId { get; set; } = string.Empty;
    }
}
