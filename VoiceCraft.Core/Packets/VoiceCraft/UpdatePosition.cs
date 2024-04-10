using System.Numerics;
using System.Text;
using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class UpdatePosition : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.UpdatePosition;
        public override bool IsReliable => false;

        public Vector3 Position { get; set; }
        public string EnvironmentId { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Position = new Vector3(BitConverter.ToSingle(dataStream, offset), BitConverter.ToSingle(dataStream, offset += sizeof(float)), BitConverter.ToSingle(dataStream, offset += sizeof(float))); //Read Position - 12 bytes.
            offset += sizeof(float);

            var environmentIdLength = BitConverter.ToInt32(dataStream, offset); //Read Environment Id Length - 4 bytes.
            offset += sizeof(int);

            if(environmentIdLength > 0)
                EnvironmentId = Encoding.UTF8.GetString(dataStream, offset, environmentIdLength); //Read Environment Id.

            offset += environmentIdLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Position.X));
            dataStream.AddRange(BitConverter.GetBytes(Position.Y));
            dataStream.AddRange(BitConverter.GetBytes(Position.Z));
            dataStream.AddRange(BitConverter.GetBytes(EnvironmentId.Length));
            if (EnvironmentId.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(EnvironmentId));
        }
    }
}