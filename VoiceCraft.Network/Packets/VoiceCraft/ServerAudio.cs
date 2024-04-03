namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class ServerAudio : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.ServerAudio;
        public override bool IsReliable => false;

        public short Key { get; set; }
        public uint PacketCount { get; set; }
        public float Volume { get; set; }
        public float EchoFactor { get; set; }
        public float Rotation { get; set; }
        public bool Muffled { get; set; }
        public byte[] Audio { get; set; } = Array.Empty<byte>();

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Key = BitConverter.ToInt16(dataStream, offset); //Read Id - 2 bytes.
            offset += sizeof(short);

            PacketCount = BitConverter.ToUInt32(dataStream, offset); //read packet count - 4 bytes.
            offset += sizeof(uint);

            Volume = BitConverter.ToSingle(dataStream, offset); //read volume - 4 bytes.
            offset += sizeof(float);

            EchoFactor = BitConverter.ToSingle(dataStream, offset); //read echo factor - 4 bytes.
            offset += sizeof(float);

            Rotation = BitConverter.ToSingle(dataStream, offset); //read rotation - 4 bytes.
            offset += sizeof(float);

            Muffled = BitConverter.ToBoolean(dataStream, offset); //read muffled - 1 byte.
            offset += sizeof(bool);

            var audioLength = BitConverter.ToInt32(dataStream, offset); //Read audio length - 4 bytes.
            offset += sizeof(int);

            if(audioLength > 0)
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
            dataStream.AddRange(BitConverter.GetBytes(Key));
            dataStream.AddRange(BitConverter.GetBytes(PacketCount));
            dataStream.AddRange(BitConverter.GetBytes(Volume));
            dataStream.AddRange(BitConverter.GetBytes(EchoFactor));
            dataStream.AddRange(BitConverter.GetBytes(Rotation));
            dataStream.AddRange(BitConverter.GetBytes(Muffled));
            dataStream.AddRange(BitConverter.GetBytes(Audio.Length));
            dataStream.AddRange(Audio);
        }
    }
}
