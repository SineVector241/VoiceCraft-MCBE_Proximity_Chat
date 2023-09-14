using System;
using System.Collections.Generic;
using VoiceCraft.Network.Packets.Interfaces;
using VoiceCraft.Network.Packets.Signalling;

namespace VoiceCraft.Network.Packets
{
    public class SignallingPacket : IPacket
    {
        public SignallingPacketTypes PacketType { get; set; }
        public IPacketData PacketData { get; set; } = new Null();

        public SignallingPacket(byte[] dataStream)
        {
            PacketType = (SignallingPacketTypes)BitConverter.ToUInt16(dataStream, 0); //Read packet type - 2 bytes.
            switch(PacketType)
            {
                case SignallingPacketTypes.Login: PacketData = new Login(dataStream, 2);
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

    public enum SignallingPacketTypes
    {
        Login,
        Logout,
        Accept,
        Deny,
        Binded,
        Deafen,
        Undeafen,
        Mute,
        Unmute,
        Error,
        Ping,
        Null
    }

    public enum PositioningTypes
    {
        ServerSided,
        ClientSided
    }
}
