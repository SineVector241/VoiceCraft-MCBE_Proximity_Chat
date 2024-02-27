using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class Login : IPacketData
    {
        public ushort Key { get; set; }

        public Login(byte[] dataStream, int readOffset = 0)
        {
            Key = BitConverter.ToUInt16(dataStream, readOffset); //read login key - 2 bytes.
        }

        public Login()
        {
            Key = 0;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(Key));

            return dataStream.ToArray();
        }

        public static VoicePacket Create(ushort loginKey)
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.Login,
                PacketData = new Login()
                {
                    Key = loginKey
                }
            };
        }
    }
}
