namespace VoiceCraft.Core.Packets.CustomClient
{
    public class Logout : CustomClientPacket
    {
        public override byte PacketId => (byte)CustomClientTypes.Logout;
    }
}
