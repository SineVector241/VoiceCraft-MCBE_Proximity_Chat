using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class Bind : IMCCommPacketData
    {
        public string PlayerId { get; set; } = string.Empty;
        public ushort PlayerKey { get; set; }
        public string Gamertag { get; set; } = string.Empty;

        public static MCCommPacket Create(string playerId, ushort playerKey, string gamertag)
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.Bind,
                PacketData = new Bind() {
                    PlayerId = playerId,
                    PlayerKey = playerKey,
                    Gamertag = gamertag
                }
            };
        }
    }
}
