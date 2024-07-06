namespace VoiceCraft.Core.Packets.MCComm
{
    public class MuteParticipant : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.MuteParticipant;
        public string PlayerId { get; set; } = string.Empty;
    }
}
