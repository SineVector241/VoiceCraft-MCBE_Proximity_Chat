using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetSettings : IMCCommPacketData
    {
        public static MCCommPacket Create()
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.GetSettings,
                PacketData = new GetSettings()
            };
        }
    }
}
