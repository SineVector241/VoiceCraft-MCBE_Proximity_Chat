using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class LeaveChannel : IPacketData
    {
        public byte ChannelId { get; set; } = 0;

        public LeaveChannel()
        {
            ChannelId = 0;
        }

        public LeaveChannel(byte[] dataStream, int readOffset = 0)
        {
            ChannelId = dataStream[readOffset]; //Read channel id - 1 byte.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>() { ChannelId };

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(byte channelId, string password)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.LeaveChannel,
                PacketData = new LeaveChannel()
                {
                    ChannelId = channelId
                }
            };
        }
    }
}
