namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class Unmute : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Unmute;
        public override bool IsReliable => true;

        public long Id { get; set; } = long.MinValue;
        public ushort Key { get; set; }

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset); //Read Id - 8 bytes.
            offset += sizeof(long);

            Key = BitConverter.ToUInt16(dataStream, offset); //Read Key - 2 bytes.
            offset += sizeof(ushort);

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
