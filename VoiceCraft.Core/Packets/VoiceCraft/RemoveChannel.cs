using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class RemoveChannel : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.RemoveChannel;
        public override bool IsReliable => true;

        public byte ChannelId { get; set; }

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            ChannelId = dataStream[offset]; //Read Channel Id - 1 byte.
            offset++;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.Add(ChannelId);
        }
    }
}
