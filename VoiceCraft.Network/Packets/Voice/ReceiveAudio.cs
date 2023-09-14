using System;
using System.Collections.Generic;
using VoiceCraft.Network.Packets.Interfaces;

namespace VoiceCraft.Network.Packets.Voice
{
    public class ReceiveAudio : IPacketData
    {
        public ushort LoginKey { get; set; }
        public uint PacketCount { get; set; }
        public float Volume { get; set; }
        public float EchoFactor { get; set; }
        public float Rotation { get; set; }
        public byte[] Audio { get; set; } = new byte[0];

        public ReceiveAudio()
        {
            LoginKey = 0;
            PacketCount = 0;
            Volume = 0;
            EchoFactor = 0;
            Rotation = 0;
            Audio = new byte[0];
        }

        public ReceiveAudio(byte[] dataStream, int readOffset = 0)
        {
            LoginKey = BitConverter.ToUInt16(dataStream, readOffset); //read login key - 2 bytes.
            PacketCount = BitConverter.ToUInt32(dataStream, readOffset + 2); //read packet count - 4 bytes.
            Volume = BitConverter.ToSingle(dataStream, readOffset + 6); //read volume - 4 bytes.
            EchoFactor = BitConverter.ToSingle(dataStream, readOffset + 10); //read echo factor - 4 bytes.
            Rotation = BitConverter.ToSingle(dataStream, readOffset + 14); //read rotation - 4 bytes.

            int audioLength = BitConverter.ToInt32(dataStream, readOffset + 18); //Read audio length - 4 bytes.

            Audio = new byte[audioLength];
            if (audioLength > 0)
                Buffer.BlockCopy(dataStream, readOffset + 22, Audio, 0, audioLength);
            else
                Audio = new byte[0];
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(LoginKey));
            dataStream.AddRange(BitConverter.GetBytes(PacketCount));
            dataStream.AddRange(BitConverter.GetBytes(Volume));
            dataStream.AddRange(BitConverter.GetBytes(EchoFactor));
            dataStream.AddRange(BitConverter.GetBytes(Volume));

            if (Audio.Length > 0)
                dataStream.AddRange(BitConverter.GetBytes(Audio.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (Audio.Length > 0)
                dataStream.AddRange(Audio);

            return dataStream.ToArray();
        }
    }
}
