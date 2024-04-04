namespace VoiceCraft.Core.Packets.MCComm
{
    public class RemoveParticipant : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketId.RemoveParticipant;
        public string PlayerId { get; set; } = string.Empty;
    }
}
