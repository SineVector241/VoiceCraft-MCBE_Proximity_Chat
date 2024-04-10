namespace VoiceCraft.Core.Packets.CustomClient
{
    public class Accept : CustomClientPacket
    {
        public override byte PacketId => (byte)CustomClientTypes.Accept;
    }
}
