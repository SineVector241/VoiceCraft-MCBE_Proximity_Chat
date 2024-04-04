using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class Ack : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Ack;
        public override bool IsReliable => false;

        public long Id { get; set; } = long.MinValue;
        public uint PacketSequence { get; set; }

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset); //Read Id - 8 bytes.
            offset += sizeof(long);

            PacketSequence = BitConverter.ToUInt32(dataStream, offset); //Read PacketSequence - 4 bytes.
            offset += sizeof(uint);

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Id));
            dataStream.AddRange(BitConverter.GetBytes(PacketSequence));
        }
    }
}
