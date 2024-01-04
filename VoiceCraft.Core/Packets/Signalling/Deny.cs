using System;
using System.Collections.Generic;
using System.Text;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Deny : IPacketData
    {
        public string Reason { get; set; } = string.Empty;
        public bool Disconnect { get; set; } = false;
        public Deny()
        {
            Reason = string.Empty;
            Disconnect = false;
        }

        public Deny(byte[] dataStream, int readOffset = 0)
        {
            Disconnect = BitConverter.ToBoolean(dataStream, readOffset); //Read disconnection - 1 byte.

            var reasonLength = BitConverter.ToInt32(dataStream, readOffset + 1); //Read reason length - 4 bytes.

            if(reasonLength > 0)
                Reason = Encoding.UTF8.GetString(dataStream, readOffset + 5, reasonLength);
            else
                Reason = string.Empty;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();
            dataStream.AddRange(BitConverter.GetBytes(Disconnect));

            if (!string.IsNullOrWhiteSpace(Reason))
                dataStream.AddRange(BitConverter.GetBytes(Reason.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrWhiteSpace(Reason))
                dataStream.AddRange(Encoding.UTF8.GetBytes(Reason));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(string reason, bool disconnect)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Deny,
                PacketData = new Deny()
                {
                    Reason = reason,
                    Disconnect = disconnect
                }
            };
        }
    }
}
