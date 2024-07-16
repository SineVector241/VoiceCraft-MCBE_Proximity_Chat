namespace VoiceCraft.Core.Packets.MCComm
{
    public class Logout : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.Logout;
    }
}
