using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class Deafen : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Deafen;
        public override bool IsReliable => true;

        public long Id { get; set; } = long.MinValue;
        public short Key { get; set; }

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset); //Read Id - 8 bytes.
            offset += sizeof(long);

            Key = BitConverter.ToInt16(dataStream, offset); //Read Key - 2 bytes.
            offset += sizeof(short);

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Id));
            dataStream.AddRange(BitConverter.GetBytes(Key));
        }
    }
}
