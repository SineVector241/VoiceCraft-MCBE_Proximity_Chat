using System.Numerics;
using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class FullUpdatePosition : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.FullUpdatePosition;
        public override bool IsReliable => false;

        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public float CaveDensity { get; set; }
        public bool IsDead { get; set; }
        public bool InWater { get; set; }

        //26 byte overhead
        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Position = new Vector3(BitConverter.ToSingle(dataStream, offset), BitConverter.ToSingle(dataStream, offset += sizeof(float)), BitConverter.ToSingle(dataStream, offset += sizeof(float))); //Read Position - 12 bytes.
            offset += sizeof(float);

            Rotation = BitConverter.ToSingle(dataStream, offset);
            offset += sizeof(float);

            CaveDensity = BitConverter.ToSingle(dataStream, offset);
            offset += sizeof(float);

            IsDead = BitConverter.ToBoolean(dataStream, offset);
            offset += sizeof(bool);

            InWater = BitConverter.ToBoolean(dataStream, offset);
            offset += sizeof(bool);

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
            dataStream.AddRange(BitConverter.GetBytes(IsDead));
            dataStream.AddRange(BitConverter.GetBytes(InWater));
        }
    }
}