namespace VoiceCraft.Core.Packets.MCComm
{
    public class Accept : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketId.Accept;
    }
}
