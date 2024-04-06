namespace VoiceCraft.Core.Packets.MCComm
{
    public class Accept : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.Accept;
    }
}
