﻿namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class LeaveChannel : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.LeaveChannel;
        public override bool IsReliable => true;

        public long Id { get; set; } = long.MinValue;
        public byte ChannelId { get; set; }

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset);
            offset += sizeof(long);

            ChannelId = dataStream[offset];
            offset++;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Id));
            dataStream.Add(ChannelId);
        }
    }
}