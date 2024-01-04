using System;
using System.Collections.Generic;
using System.Text;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class Deny : IPacketData
    {
        public string Reason { get; set; } = string.Empty;
        public Deny()
        {
            Reason = string.Empty;
        }

        public Deny(byte[] dataStream, int readOffset = 0)
        {
            var reasonLength = BitConverter.ToInt32(dataStream, readOffset); //Read reason length - 4 bytes.

            if(reasonLength > 0)
                Reason = Encoding.UTF8.GetString(dataStream, readOffset + 4, reasonLength);
            else
                Reason = string.Empty;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            if (!string.IsNullOrWhiteSpace(Reason))
                dataStream.AddRange(BitConverter.GetBytes(Reason.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrWhiteSpace(Reason))
                dataStream.AddRange(Encoding.UTF8.GetBytes(Reason));

            return dataStream.ToArray();
        }

        public static VoicePacket Create(string reason)
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.Deny,
                PacketData = new Deny()
                {
                    Reason = reason
                }
            };
        }
    }
}
