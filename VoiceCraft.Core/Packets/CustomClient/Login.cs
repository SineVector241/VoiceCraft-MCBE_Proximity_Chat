using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceCraft.Core.Packets.CustomClient
{
    public class Login : CustomClientPacket
    {
        public override byte PacketId => (byte)CustomClientTypes.Login;

        public string Name { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            var nameLength = BitConverter.ToInt32(dataStream, offset);
            offset += sizeof(int);

            if(nameLength > 0)
                Encoding.UTF8.GetString(dataStream, offset, nameLength);

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
