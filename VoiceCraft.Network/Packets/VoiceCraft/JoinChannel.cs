using System.Text;

namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class JoinChannel : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.JoinChannel;
        public override bool IsReliable => true;

        public long Id { get; set; } = long.MinValue;
        public byte ChannelId { get; set; }
        public string Password { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset);
            offset += sizeof(long);

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
            dataStream.AddRange(BitConverter.GetBytes(Id));
            dataStream.Add(ChannelId);
            dataStream.AddRange(BitConverter.GetBytes(Password.Length));
            if(Password.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Password));
        }
    }
}
