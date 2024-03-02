using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class AcceptUpdate : IMCCommPacketData
    {
        public List<string> SpeakingPlayers = new List<string>();
        public static MCCommPacket Create(List<string> data)
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.AcceptUpdate,
                PacketData = new AcceptUpdate()
                {
                    SpeakingPlayers = data;
                }
            };
        }
    }
}
