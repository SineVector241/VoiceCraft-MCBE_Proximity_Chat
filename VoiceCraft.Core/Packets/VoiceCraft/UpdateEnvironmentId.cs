using System.Collections.Generic;
using System.Text;
using System;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class UpdateEnvironmentId : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.UpdateEnvironmentId;
        public override bool IsReliable => true;

        public string EnvironmentId { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            var environmentIdLength = BitConverter.ToInt32(dataStream, offset); //Read Environment Id Length - 4 bytes.
            offset += sizeof(int);

            if (environmentIdLength > 0)
                EnvironmentId = Encoding.UTF8.GetString(dataStream, offset, environmentIdLength); //Read Environment Id.

            offset += environmentIdLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(EnvironmentId.Length));
            if (EnvironmentId.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(EnvironmentId));
        }
    }
}
