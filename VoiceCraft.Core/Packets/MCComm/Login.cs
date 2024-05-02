namespace VoiceCraft.Core.Packets.MCComm
{
    public class Login : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.Login;
        public string LoginKey { get; set; } = string.Empty;
    }
}
