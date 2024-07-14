using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class ServerAudio : VoiceCraftPacket
    {
        const int Packed8BitLimit = 256; //2 ^ 8
        const int Packed16BitLimit = 65536; //2 ^ 16
        public override byte PacketId => (byte)VoiceCraftPacketTypes.ServerAudio;
        public override bool IsReliable => false;

        public short Key { get; set; }
        public uint PacketCount { get; set; }
        public float Volume { get; set; }
        public float Rotation { get; set; }
        public float EchoFactor { get; set; }
        public bool Muffled { get; set; }
        public byte[] Audio { get; set; } = Array.Empty<byte>();

        //16 byte overhead
        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Key = BitConverter.ToInt16(dataStream, offset); //Read Id - 2 bytes.
            offset += sizeof(short);

            PacketCount = BitConverter.ToUInt32(dataStream, offset); //read packet count - 4 bytes.
            offset += sizeof(uint);

            var packedVolume = BitConverter.ToUInt16(dataStream, offset); //read volume - 2 bytes.
            Volume = packedVolume / (float)Packed16BitLimit;
            offset += sizeof(ushort);

            Rotation = BitConverter.ToSingle(dataStream, offset); //read rotation - 4 bytes.
            offset += sizeof(float);

            var packedEcho = dataStream[offset]; //read echo factor = 1 byte.
            EchoFactor = packedEcho / (float)Packed8BitLimit;
            offset++;

            Muffled = BitConverter.ToBoolean(dataStream, offset);
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
            dataStream.AddRange(BitConverter.GetBytes((ushort)(Volume * Packed16BitLimit)));
            dataStream.AddRange(BitConverter.GetBytes(Rotation));
            dataStream.Add((byte)(EchoFactor * Packed8BitLimit));
            dataStream.AddRange(BitConverter.GetBytes(Muffled));
            dataStream.AddRange(BitConverter.GetBytes(Audio.Length));
            dataStream.AddRange(Audio);
        }
    }
}
