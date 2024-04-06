namespace VoiceCraft.Core.Packets.MCComm
{
    public class Deny : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.Deny;
        public string Reason { get; set; } = string.Empty;
    }
}
