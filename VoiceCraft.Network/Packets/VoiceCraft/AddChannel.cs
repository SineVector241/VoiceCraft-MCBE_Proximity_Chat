using System.Text;

namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class AddChannel : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.AddChannel;
        public override bool IsReliable => true;

        public long Id { get; set; } = long.MinValue;
        public bool RequiresPassword { get; set; }
        public byte ChannelId { get; set; }
        public string Name { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset); //Read Id - 8 bytes.
            offset += sizeof(long);

            RequiresPassword = BitConverter.ToBoolean(dataStream, offset); //Read Password Requirement - 1 byte.
            offset += sizeof(bool);

            ChannelId = dataStream[offset]; //Read Channel Id - 1 byte.
            offset++;

            var nameLength = BitConverter.ToInt32(dataStream, offset); //Read Name Length - 4 bytes.
            offset += sizeof(int);

            if(nameLength > 0) 
                Name = Encoding.UTF8.GetString(dataStream, offset, nameLength); //Read Name.

            offset += nameLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Id));
            dataStream.AddRange(BitConverter.GetBytes(RequiresPassword));
            dataStream.Add(ChannelId);
            dataStream.AddRange(BitConverter.GetBytes(Name.Length));
            if(Name.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Name));
        }
    }
}
