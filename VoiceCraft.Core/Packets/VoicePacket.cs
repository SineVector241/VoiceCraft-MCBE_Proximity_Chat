using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.Voice;

namespace VoiceCraft.Core.Packets
{
    public class VoicePacket : IVoicePacket
    {
        public VoicePacketTypes PacketType { get; set; }
        public IPacketData PacketData { get; set; } = new Null();

        public VoicePacket()
        {
            PacketType = VoicePacketTypes.Null;
        }

        public VoicePacket(byte[] dataStream)
        {
            PacketType = (VoicePacketTypes)BitConverter.ToUInt16(dataStream, 0); //Read packet type - 2 bytes.
            switch (PacketType)
            {
                case VoicePacketTypes.Login: PacketData = new Login(dataStream, 2);
                    break;
                case VoicePacketTypes.Deny: PacketData = new Deny(dataStream, 2);
                    break;
                case VoicePacketTypes.ClientAudio: PacketData = new ClientAudio(dataStream, 2);
                    break;
                case VoicePacketTypes.ServerAudio: PacketData = new ServerAudio(dataStream, 2);
                    break;
                case VoicePacketTypes.UpdatePosition: PacketData = new UpdatePosition(dataStream, 2);
                    break;
                case VoicePacketTypes.KeepAlive: PacketData = new KeepAlive(dataStream, 2);
                    break;
                default: PacketData = new Null();
                    break;
            }
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes((ushort)PacketType));
            dataStream.AddRange(PacketData?.GetPacketStream());

            return dataStream.ToArray();
        }
    }

    public enum VoicePacketTypes
    {
        Login,
        Accept,
        Deny,
        ClientAudio,
        ServerAudio,
        UpdatePosition,
        KeepAlive,
        Null
    }
}
