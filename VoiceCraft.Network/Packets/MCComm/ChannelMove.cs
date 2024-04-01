using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class ChannelMove : IMCCommPacketData
    {
        public string PlayerId { get; set; } = string.Empty;
        public byte ChannelId { get; set; } = 0; //Set to 0 to disconnect the participant from the channel.

        public static MCCommPacket Create(string playerId, byte channelId)
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.ChannelMove,
                PacketData = new ChannelMove() { PlayerId = playerId, ChannelId = channelId }
            };
        }
    }
}
