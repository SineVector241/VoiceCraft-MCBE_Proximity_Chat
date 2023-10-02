using System;
using System.Collections.Generic;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.Signalling;

namespace VoiceCraft.Core.Packets
{
    public class SignallingPacket : ISignallingPacket
    {
        public SignallingPacketTypes PacketType { get; set; }
        public IPacketData PacketData { get; set; } = new Null();

        public SignallingPacket()
        {
            PacketType = SignallingPacketTypes.Null;
        }

        public SignallingPacket(byte[] dataStream)
        {
            PacketType = (SignallingPacketTypes)BitConverter.ToUInt16(dataStream, 0); //Read packet type - 2 bytes.
            switch(PacketType)
            {
                case SignallingPacketTypes.Login: PacketData = new Login(dataStream, 2);
                    break;
                case SignallingPacketTypes.Logout: PacketData = new Logout(dataStream, 2); 
                    break;
                case SignallingPacketTypes.Accept: PacketData = new Accept(dataStream, 2);
                    break;
                case SignallingPacketTypes.Deny: PacketData = new Deny(dataStream, 2);
                    break;
                case SignallingPacketTypes.Binded: PacketData = new Binded(dataStream, 2);
                    break;
                case SignallingPacketTypes.Unbinded: PacketData = new Unbinded();
                    break;
                case SignallingPacketTypes.Deafen: PacketData = new Deafen(dataStream, 2);
                    break;
                case SignallingPacketTypes.Undeafen: PacketData = new Undeafen(dataStream, 2);
                    break;
                case SignallingPacketTypes.Mute: PacketData = new Mute(dataStream, 2);
                    break;
                case SignallingPacketTypes.Unmute: PacketData = new Unmute(dataStream, 2);
                    break;
                case SignallingPacketTypes.Error: PacketData = new Error(dataStream, 2);
                    break;
                case SignallingPacketTypes.Ping: PacketData = new Ping(dataStream, 2);
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

        public static ushort GetPacketLength(byte[] dataStream)
        {
            return BitConverter.ToUInt16(dataStream, 0);
        }
    }

    public enum SignallingPacketTypes
    {
        Login,
        Logout,
        Accept,
        Deny,
        Binded,
        Unbinded,
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
