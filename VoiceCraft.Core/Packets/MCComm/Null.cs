using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Null : IMCCommPacketData
    {
        public static MCCommPacket Create()
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.Null,
                PacketData = new Null()
            };
        }
    }
}
