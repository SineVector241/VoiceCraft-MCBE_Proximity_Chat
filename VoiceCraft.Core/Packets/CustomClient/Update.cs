using System.Collections.Generic;
using System;
using System.Numerics;
using System.Text;

namespace VoiceCraft.Core.Packets.CustomClient
{
    public class Update : CustomClientPacket
    {
        public override byte PacketId => (byte)CustomClientTypes.Update;

        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public float CaveDensity { get; set; }
        public bool IsUnderwater { get; set; }
        public string DimensionId { get; set; } = string.Empty;
        public string LevelId { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Position = new Vector3(BitConverter.ToSingle(dataStream, offset), BitConverter.ToSingle(dataStream, offset += sizeof(float)), BitConverter.ToSingle(dataStream, offset += sizeof(float)));
            offset += sizeof(float);

            Rotation = BitConverter.ToSingle(dataStream, offset);
            offset += sizeof(float);

            CaveDensity = BitConverter.ToSingle(dataStream, offset);
            offset += sizeof(float);

            IsUnderwater = BitConverter.ToBoolean(dataStream, offset);
            offset += sizeof(bool);

            var dimensionIdLength = BitConverter.ToInt32(dataStream, offset);
            offset += sizeof(int);

            if (dimensionIdLength > 0)
                DimensionId = Encoding.UTF8.GetString(dataStream, offset, dimensionIdLength);

            offset += dimensionIdLength;

            var levelIdLength = BitConverter.ToInt32(dataStream, offset);
            offset += sizeof(int);

            if (levelIdLength > 0)
                LevelId = Encoding.UTF8.GetString(dataStream, offset, levelIdLength);

            offset += levelIdLength;

            var serverIdLength = BitConverter.ToInt32(dataStream, offset);
            offset += sizeof(int);

            if (serverIdLength > 0)
                Encoding.UTF8.GetString(dataStream, offset, serverIdLength);

            offset += serverIdLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Position.X));
            dataStream.AddRange(BitConverter.GetBytes(Position.Y));
            dataStream.AddRange(BitConverter.GetBytes(Position.Z));
            dataStream.AddRange(BitConverter.GetBytes(Rotation));
            dataStream.AddRange(BitConverter.GetBytes(CaveDensity));
            dataStream.AddRange(BitConverter.GetBytes(IsUnderwater));
            dataStream.AddRange(BitConverter.GetBytes(DimensionId.Length));

            if(DimensionId.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(DimensionId));

            dataStream.AddRange(BitConverter.GetBytes(LevelId.Length));

            if(LevelId.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(LevelId));

            dataStream.AddRange(BitConverter.GetBytes(ServerId.Length));

            if(ServerId.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(ServerId));
        }
    }
}
