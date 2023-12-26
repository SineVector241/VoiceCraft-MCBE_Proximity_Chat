using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Deny : IMCCommPacketData
    {
        public string Reason { get; set; } = string.Empty;

        public static MCCommPacket Create(string reason)
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.Deny,
                PacketData = new Deny()
                {
                    Reason = reason
                }
            };
        }
    }
}
