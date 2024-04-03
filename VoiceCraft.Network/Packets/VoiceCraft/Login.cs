using System.Text;
using VoiceCraft.Core;

namespace VoiceCraft.Network.Packets.VoiceCraft
{
    public class Login : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Login;
        public override bool IsReliable => true;

        //Packet Variables
        public short Key { get; set; }
        public PositioningTypes PositioningType { get; set; }
        public string Version { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            Key = BitConverter.ToInt16(dataStream, offset); //Read Key - 2 bytes.
            offset += sizeof(short);

            PositioningType = (PositioningTypes)dataStream[offset]; //Read PositioningType - 1 byte.
            offset++;

            var versionLength = BitConverter.ToInt32(dataStream, offset); //Read Name length - 4 bytes.
            offset += sizeof(int);

            if (versionLength > 0)
                Version = Encoding.UTF8.GetString(dataStream, offset, versionLength); //Read Version string.

            offset += versionLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.AddRange(BitConverter.GetBytes(Key));
            dataStream.Add((byte)PositioningType);
            dataStream.AddRange(BitConverter.GetBytes(Version.Length));
            if (Version.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(Version));
        }
    }
}
