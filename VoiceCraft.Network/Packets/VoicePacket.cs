using System;
using System.Collections.Generic;
using VoiceCraft.Network.Packets.Interfaces;
using VoiceCraft.Network.Packets.Voice;

namespace VoiceCraft.Network.Packets
{
    public class VoicePacket : IPacket
    {
        public SignallingPacketTypes PacketType { get; set; }
        public IPacketData PacketData { get; set; } = new Null();

        public VoicePacket(byte[] dataStream)
        {
            PacketType = (SignallingPacketTypes)BitConverter.ToUInt16(dataStream, 0); //Read packet type - 2 bytes.
            switch (PacketType)
            {
                case SignallingPacketTypes.Login:
                    PacketData = new Login();
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
        SendAudio,
        ReceiveAudio,
        UpdatePosition,
        Null
    }
}
