using System.Collections.Generic;
using System.Text;
using System;

namespace VoiceCraft.Core.Packets.CustomClient
{
    public class Deny : CustomClientPacket
    {
        public override byte PacketId => (byte)CustomClientTypes.Deny;

        public string Reason { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            var reasonLength = BitConverter.ToInt32(dataStream, offset);
            offset += sizeof(int);

            if (reasonLength > 0)
                Encoding.UTF8.GetString(dataStream, offset, reasonLength);

            offset += reasonLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Reason.Length));
            if (Reason.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Reason));
        }
    }
}
