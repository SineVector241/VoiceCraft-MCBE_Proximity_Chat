using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class ClientAudio : IPacketData
    {
        public uint PacketCount = 0;
        public byte[] Audio = new byte[0];

        public ClientAudio()
        {
            PacketCount = 0;
            Audio = new byte[0];
        }

        public ClientAudio(byte[] dataStream, int readOffset = 0)
        {
            PacketCount = BitConverter.ToUInt32 (dataStream, readOffset); //Read packet count - 4 bytes.
            int audioLength = BitConverter.ToInt32(dataStream, readOffset + 4); //Read audio length - 4 bytes.

            Audio = new byte[audioLength];
            if(audioLength > 0)
                Buffer.BlockCopy(dataStream, readOffset + 8, Audio, 0, audioLength);
            else
                Audio = new byte[0];
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PacketCount));

            if (Audio.Length > 0)
                dataStream.AddRange(BitConverter.GetBytes(Audio.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (Audio.Length > 0)
                dataStream.AddRange(Audio);

            return dataStream.ToArray();
        }

        public static VoicePacket Create(uint packetCount, byte[] audio)
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.ClientAudio,
                PacketData = new ClientAudio()
                {
                    PacketCount = packetCount,
                    Audio = audio,
                }
            };
        }
    }
}
