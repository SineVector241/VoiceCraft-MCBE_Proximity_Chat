namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetSettings : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketId.GetSettings;
    }
}
