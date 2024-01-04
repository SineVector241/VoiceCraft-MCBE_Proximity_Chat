using System.Collections.Generic;
using System;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class Accept : IPacketData
    {
        public ushort LoginKey { get; set; }
        public ushort VoicePort { get; set; }

        public Accept()
        {
            LoginKey = 0;
            VoicePort = 0;
        }

        public Accept(byte[] dataStream, int readOffset = 0)
        {
            LoginKey = BitConverter.ToUInt16(dataStream, readOffset); //Read login key - 2 bytes.
            VoicePort = BitConverter.ToUInt16(dataStream, readOffset + 2); //Read voice port - 2 bytes.
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(LoginKey));
            dataStream.AddRange(BitConverter.GetBytes(VoicePort));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(ushort loginKey, ushort voicePort)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.Accept,
                PacketData = new Accept()
                {
                    LoginKey = loginKey,
                    VoicePort = voicePort
                }
            };
        }
    }
}
