using System.Text;
using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class JoinChannel : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.JoinChannel;
        public override bool IsReliable => true;

        public byte ChannelId { get; set; }
        public string Password { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            ChannelId = dataStream[offset];
            offset++;

            var passwordLength = BitConverter.ToInt32(dataStream, offset);
            offset += sizeof(int);

            if(passwordLength > 0)
                Password = Encoding.UTF8.GetString(dataStream, offset, passwordLength);

            offset += passwordLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.Add(ChannelId);
            dataStream.AddRange(BitConverter.GetBytes(Password.Length));
            if(Password.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Password));
        }
    }
}
