using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class ServerAudio : IPacketData
    {
        public ushort Key { get; set; }
        public uint PacketCount { get; set; }
        public float Volume { get; set; }
        public float EchoFactor { get; set; }
        public float Rotation { get; set; }
        public bool Muffled { get; set; }
        public byte[] Audio { get; set; } = new byte[0];

        public ServerAudio()
        {
            Key = 0;
            PacketCount = 0;
            Volume = 0;
            EchoFactor = 0;
            Rotation = 0;
            Muffled = false;
            Audio = new byte[0];
        }

        public ServerAudio(byte[] dataStream, int readOffset = 0)
        {
            Key = BitConverter.ToUInt16(dataStream, readOffset); //read login key - 2 bytes.
            PacketCount = BitConverter.ToUInt32(dataStream, readOffset + 2); //read packet count - 4 bytes.
            Volume = BitConverter.ToSingle(dataStream, readOffset + 6); //read volume - 4 bytes.
            EchoFactor = BitConverter.ToSingle(dataStream, readOffset + 10); //read echo factor - 4 bytes.
            Rotation = BitConverter.ToSingle(dataStream, readOffset + 14); //read rotation - 4 bytes.
            Muffled = BitConverter.ToBoolean(dataStream, readOffset + 18); //read muffled - 1 byte.

            int audioLength = BitConverter.ToInt32(dataStream, readOffset + 19); //Read audio length - 4 bytes.

            Audio = new byte[audioLength];
            if (audioLength > 0)
                Buffer.BlockCopy(dataStream, readOffset + 23, Audio, 0, audioLength);
            else
                Audio = new byte[0];
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(Key));
            dataStream.AddRange(BitConverter.GetBytes(PacketCount));
            dataStream.AddRange(BitConverter.GetBytes(Volume));
            dataStream.AddRange(BitConverter.GetBytes(EchoFactor));
            dataStream.AddRange(BitConverter.GetBytes(Rotation));
            dataStream.AddRange(BitConverter.GetBytes(Muffled));

            if (Audio.Length > 0)
                dataStream.AddRange(BitConverter.GetBytes(Audio.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (Audio.Length > 0)
                dataStream.AddRange(Audio);

            return dataStream.ToArray();
        }

        public static VoicePacket Create(ushort loginKey, uint packetCount, float volume, float echoFactor, float rotation, bool muffled, byte[] audio)
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.ServerAudio,
                PacketData = new ServerAudio()
                {
                    Key = loginKey,
                    PacketCount = packetCount,
                    Volume = volume,
                    EchoFactor = echoFactor,
                    Rotation = rotation,
                    Muffled = muffled,
                    Audio = audio
                }
            };
        }
    }
}
