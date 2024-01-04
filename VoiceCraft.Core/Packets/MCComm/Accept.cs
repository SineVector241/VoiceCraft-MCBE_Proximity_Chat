using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Accept : IMCCommPacketData
    {
        public static MCCommPacket Create()
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.Accept,
                PacketData = new Accept()
            };
        }
    }
}
