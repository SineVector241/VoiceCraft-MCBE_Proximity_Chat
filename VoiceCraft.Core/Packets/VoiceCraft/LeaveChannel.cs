using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class LeaveChannel : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.LeaveChannel;
        public override bool IsReliable => true;

        public byte ChannelId { get; set; }

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            ChannelId = dataStream[offset];
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