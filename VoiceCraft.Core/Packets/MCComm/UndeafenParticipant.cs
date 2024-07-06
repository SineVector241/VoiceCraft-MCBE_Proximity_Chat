namespace VoiceCraft.Core.Packets.MCComm
{
    public class UndeafenParticipant : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.UndeafenParticipant;
        public string PlayerId { get; set; } = string.Empty;
    }
}
