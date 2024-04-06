using System.Collections.Generic;
using System.Text;
using System;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class Binded : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Binded;
        public override bool IsReliable => true;

        public string Name { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            var nameLength = BitConverter.ToInt32(dataStream, offset); //Read Name length - 4 bytes.
            offset += sizeof(int);

            if (nameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, offset, nameLength); //Read Name.

            offset += nameLength;
            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Name.Length));
            if (Name.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Name));
        }
    }
}
