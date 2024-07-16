namespace VoiceCraft.Core.Packets.MCComm
{
    public class DisconnectParticipant : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.DisconnectParticipant;
        public string PlayerId { get; set; } = string.Empty;
    }
}
