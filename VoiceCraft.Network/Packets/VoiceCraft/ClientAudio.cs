namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class ClientAudio : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.ClientAudio;
        public override bool IsReliable => false;

        public long Id { get; set; } = long.MinValue;
        public uint PacketCount { get; set; }
        public byte[] Audio { get; set; } = Array.Empty<byte>();

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Id = BitConverter.ToInt64(dataStream, offset); //Read Id - 8 bytes.
            offset += sizeof(long);

            PacketCount = BitConverter.ToUInt32(dataStream, offset); //Read Packet Count - 4 bytes.
            offset += sizeof(uint);

            var audioLength = BitConverter.ToInt32(dataStream, offset); //Read Audio Length - 4 bytes.
            offset += sizeof(int);

            if (audioLength > 0)
            {
                Audio = new byte[audioLength];
                Buffer.BlockCopy(dataStream, offset, Audio, 0, audioLength);
            }

            offset += audioLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Id));
            dataStream.AddRange(BitConverter.GetBytes(PacketCount));
            dataStream.AddRange(BitConverter.GetBytes(Audio.Length));
            dataStream.AddRange(Audio);
        }
    }
}
