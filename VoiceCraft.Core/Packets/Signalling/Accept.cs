using System.Collections.Generic;
using System;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Accept : IPacketData
    {
        public int PrivateId { get; set; }
        public ushort PublicId { get; set; }
        public ushort VoicePort { get; set; }

        public Accept()
        {
            PrivateId = 0;
            PublicId = 0;
            VoicePort = 0;
        }

        public Accept(byte[] dataStream, int readOffset = 0)
        {
            PrivateId = BitConverter.ToInt32(dataStream, readOffset); //Read login Id - 4 bytes.
            PublicId = BitConverter.ToUInt16(dataStream, readOffset + 4); //Read login key - 2 bytes.
            VoicePort = BitConverter.ToUInt16(dataStream, readOffset + 6); //Read voice port - 2 bytes.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(PrivateId));
            dataStream.AddRange(BitConverter.GetBytes(PublicId));
            dataStream.AddRange(BitConverter.GetBytes(VoicePort));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(int privateId, ushort publicId, ushort voicePort)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Accept,
                PacketData = new Accept()
                {
                    PrivateId = privateId,
                    PublicId = publicId,
                    VoicePort = voicePort
                }
            };
        }
    }
}
