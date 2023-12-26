using System;
using System.Collections.Generic;
using System.Text;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Signalling
{
    public class AddChannel : IPacketData
    {
        public string Name { get; set; } = string.Empty;
        public byte ChannelId { get; set; } = 0;
        public bool RequiresPassword { get; set; }

        public AddChannel()
        {
            Name = string.Empty;
            ChannelId = 0;
            RequiresPassword = false;
        }

        public AddChannel(byte[] dataStream, int readOffset = 0)
        {
            RequiresPassword = BitConverter.ToBoolean(dataStream, readOffset); //Read password requirement - 1 byte.
            ChannelId = dataStream[readOffset + 1]; //Read channel id - 1 byte.

            int nameLength = BitConverter.ToInt32(dataStream, readOffset + 2); //read name length - 4 bytes.

            if (nameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, readOffset + 6, nameLength);
            else
                Name = string.Empty;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(RequiresPassword));
            dataStream.Add(ChannelId);

            if(Name.Length > 0)
                dataStream.AddRange(BitConverter.GetBytes(Name.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (Name.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Name));

            return dataStream.ToArray();
        }

        public static SignallingPacket Create(string name, byte channelId, bool requiresPassword)
        {
            return new SignallingPacket()
            {
                PacketType = SignallingPacketTypes.AddChannel,
                PacketData = new AddChannel()
                {
                    Name = name,
                    ChannelId = channelId,
                    RequiresPassword = requiresPassword
                }
            };
        }
    }
}
