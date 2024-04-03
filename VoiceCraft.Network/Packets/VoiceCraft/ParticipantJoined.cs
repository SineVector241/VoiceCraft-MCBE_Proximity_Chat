using System.Text;

namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class ParticipantJoined : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.ParticipantJoined;
        public override bool IsReliable => true;

        public long Id { get; set; } = long.MinValue;
        public short Key { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsMuted { get; set; }
        public string Name { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset); //Read Id - 8 bytes.
            offset += sizeof(long);

            Key = BitConverter.ToInt16(dataStream, offset); //Read Key - 2 bytes.
            offset += sizeof(short);

            IsDeafened = BitConverter.ToBoolean(dataStream, offset); //Read Deafened State - 1 byte.
            offset += sizeof(bool);

            IsMuted = BitConverter.ToBoolean(dataStream, offset); //Read Muted State - 1 byte.
            offset += sizeof(bool);

            var nameLength = BitConverter.ToInt32(dataStream, offset);
            offset += sizeof(int);

            if(nameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, offset, nameLength);

            offset += nameLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Id));
            dataStream.AddRange(BitConverter.GetBytes(Key));
            dataStream.AddRange(BitConverter.GetBytes(IsDeafened));
            dataStream.AddRange(BitConverter.GetBytes(IsMuted));
            dataStream.AddRange(BitConverter.GetBytes(Name.Length));
            if(Name.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Name));
        }
    }
}
