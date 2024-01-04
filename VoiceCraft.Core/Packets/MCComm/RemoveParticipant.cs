using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class RemoveParticipant : IMCCommPacketData
    {
        public string PlayerId { get; set; } = string.Empty;

        public static MCCommPacket Create(string playerId)
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.RemoveParticipant,
                PacketData = new RemoveParticipant()
                {
                    PlayerId = playerId
                }
            };
        }
    }
}
