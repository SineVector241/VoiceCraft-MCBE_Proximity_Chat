using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class ClientAudio : IPacketData
    {
        public int PrivateId { get; set; } = 0;
        public uint PacketCount { get; set; } = 0;
        public byte[] Audio { get; set; } = new byte[0];

        public ClientAudio()
        {
            PrivateId = 0;
            PacketCount = 0;
            Audio = new byte[0];
        }

        public ClientAudio(byte[] dataStream, int readOffset = 0)
        {
            PrivateId = BitConverter.ToInt32(dataStream, readOffset); //Read login Id - 4 bytes.
            PacketCount = BitConverter.ToUInt32(dataStream, readOffset + 4); //Read packet count - 4 bytes.
            int audioLength = BitConverter.ToInt32(dataStream, readOffset + 8); //Read audio length - 4 bytes.

            Audio = new byte[audioLength];
            if(audioLength > 0)
                Buffer.BlockCopy(dataStream, readOffset + 10, Audio, 0, audioLength);
            else
                Audio = new byte[0];
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PrivateId));
            dataStream.AddRange(BitConverter.GetBytes(PacketCount));

            if (Audio.Length > 0)
                dataStream.AddRange(BitConverter.GetBytes(Audio.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (Audio.Length > 0)
                dataStream.AddRange(Audio);

            return dataStream.ToArray();
        }

        public static VoicePacket Create(int privateId, uint packetCount, byte[] audio)
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.ClientAudio,
                PacketData = new ClientAudio()
                {
                    PrivateId = privateId,
                    PacketCount = packetCount,
                    Audio = audio
                }
            };
        }
    }
}
