using System;
using System.Collections.Generic;
using System.Text;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Ping : IPacketData
    {
        public string ServerData { get; set; } = string.Empty;

        public Ping()
        {
            ServerData = string.Empty;
        }

        public Ping(byte[] dataStream, int readOffset = 0)
        {
            var serverDataLength = BitConverter.ToInt32(dataStream, readOffset); //Read server data length - 4 bytes.

            if (serverDataLength > 0)
                ServerData = Encoding.UTF8.GetString(dataStream, readOffset + 4, serverDataLength);
            else
                ServerData = string.Empty;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            if (!string.IsNullOrEmpty(ServerData))
                dataStream.AddRange(BitConverter.GetBytes(ServerData.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrEmpty(ServerData))
                dataStream.AddRange(Encoding.UTF8.GetBytes(ServerData));

            return dataStream.ToArray();
        }
    }
}
