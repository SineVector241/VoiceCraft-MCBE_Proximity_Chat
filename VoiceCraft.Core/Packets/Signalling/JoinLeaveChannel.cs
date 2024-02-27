using System;
using System.Collections.Generic;
using System.Text;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class JoinLeaveChannel : IPacketData
    {
        public byte ChannelId { get; set; } = 0;
        public bool JoiningChannel { get; set; } = true;
        public string Password { get; set; } = string.Empty;

        public JoinLeaveChannel()
        {
            ChannelId = 0;
            Password = string.Empty;
            JoiningChannel = true;
        }

        public JoinLeaveChannel(byte[] dataStream, int readOffset = 0)
        {
            ChannelId = dataStream[readOffset]; //Read channel id - 1 byte.
            JoiningChannel = BitConverter.ToBoolean(dataStream, readOffset + 1); //Read if joining - 1 byte.

            int passwordLength = BitConverter.ToInt32(dataStream, readOffset + 2); //read name length - 4 bytes.

            if (passwordLength > 0)
                Password = Encoding.UTF8.GetString(dataStream, readOffset + 6, passwordLength);
            else
                Password = string.Empty;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>() { ChannelId };

            dataStream.AddRange(BitConverter.GetBytes(JoiningChannel));

            if (Password.Length > 0)
                dataStream.AddRange(BitConverter.GetBytes(Password.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (Password.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Password));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(byte channelId, string password, bool isJoining)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.JoinChannel,
                PacketData = new JoinLeaveChannel()
                {
                    ChannelId = channelId,
                    Password = password,
                    JoiningChannel = isJoining
                }
            };
        }
    }
}
