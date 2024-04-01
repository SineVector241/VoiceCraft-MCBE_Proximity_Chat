using System.Collections.Generic;
using System.Numerics;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Update : IMCCommPacketData
    {
        public List<Player> Players { get; set; } = new List<Player>();

        public static MCCommPacket Create(List<Player> players)
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.Update,
                PacketData = new Update()
                {
                    Players = players
                }
            };
        }
    }

    public class Player
    {
        public string PlayerId { get; set; } = string.Empty;
        public string DimensionId { get; set; } = string.Empty;
        public Vector3 Location { get; set; } = new Vector3();
        public float Rotation { get; set; }
        public float CaveDensity { get; set; }
        public bool IsDead { get; set; }
        public bool InWater { get; set; }
    }
}
