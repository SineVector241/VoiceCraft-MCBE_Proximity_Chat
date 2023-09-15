using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class ClientAudio : IPacketData
    {
        public byte[] Audio = new byte[0];

        public ClientAudio()
        {
            Audio = new byte[0];
        }

        public ClientAudio(byte[] dataStream, int readOffset = 0)
        {
            int audioLength = BitConverter.ToInt32(dataStream, readOffset); //Read audio length - 4 bytes.

            Audio = new byte[audioLength];
            if(audioLength > 0)
                Buffer.BlockCopy(dataStream, readOffset + 4, Audio, 0, audioLength);
            else
                Audio = new byte[0];
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

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
