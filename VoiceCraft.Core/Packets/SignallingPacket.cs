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
                case SignallingPacketTypes.BindedUnbinded: PacketData = new BindedUnbinded(dataStream, 2);
                    break;
                case SignallingPacketTypes.DeafenUndeafen: PacketData = new DeafenUndeafen(dataStream, 2);
                    break;
                case SignallingPacketTypes.MuteUnmute: PacketData = new MuteUnmute(dataStream, 2);
                    break;
                case SignallingPacketTypes.AddChannel: PacketData = new AddChannel(dataStream, 2);
                    break;
                case SignallingPacketTypes.JoinLeaveChannel: PacketData = new JoinLeaveChannel(dataStream, 2);
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
        //Login Protocol
        Login,
        Logout,
        Accept,
        Deny,
        BindedUnbinded,

        //States
        DeafenUndeafen,
        MuteUnmute,

        //Channels
        AddChannel,
        JoinLeaveChannel,

        //Other stuff
        Error,
        Ping,
        PingCheck,
        Null
    }
}
