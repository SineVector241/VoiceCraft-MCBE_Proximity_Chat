using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.Voice
{
    public class UpdatePosition : IPacketData
    {
        public Vector3 Position { get; set; }
        public string EnvironmentId { get; set; } = string.Empty;

        public UpdatePosition()
        {
            Position = new Vector3(0,0,0);
            EnvironmentId = string.Empty;
        }

        public UpdatePosition(byte[] dataStream, int readOffset = 0)
        {
            Position = new Vector3(BitConverter.ToSingle(dataStream, readOffset), BitConverter.ToSingle(dataStream, readOffset + 4), BitConverter.ToSingle(dataStream, readOffset + 8)); //Read position - 12 bytes.

            int environmentIdLength = BitConverter.ToInt32(dataStream, readOffset + 12); //read environment id length - 4 bytes.

            if (environmentIdLength > 0)
                EnvironmentId = Encoding.UTF8.GetString(dataStream, readOffset + 16, environmentIdLength);
            else
                EnvironmentId = string.Empty;
        }

        public byte[] GetPacketStream()
        {
            var dataStream = new List<byte>();

            dataStream.AddRange(BitConverter.GetBytes(Position.X)); //Packet Position X
            dataStream.AddRange(BitConverter.GetBytes(Position.Y)); //Packet Position Y
            dataStream.AddRange(BitConverter.GetBytes(Position.Z)); //Packet Position Z

            if (!string.IsNullOrWhiteSpace(EnvironmentId))
                dataStream.AddRange(BitConverter.GetBytes(EnvironmentId.Length));
            else
                dataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrWhiteSpace(EnvironmentId))
                dataStream.AddRange(Encoding.UTF8.GetBytes(EnvironmentId));

            return dataStream.ToArray();
        }

        public static VoicePacket Create(Vector3 position, string environmentId)
        {
            return new VoicePacket()
            {
                PacketType = VoicePacketTypes.UpdatePosition,
                PacketData = new UpdatePosition()
                {
                    Position = position,
                    EnvironmentId = environmentId
                }
            };
        }
    }
}
