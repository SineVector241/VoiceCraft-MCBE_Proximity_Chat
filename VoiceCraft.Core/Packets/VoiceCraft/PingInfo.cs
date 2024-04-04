using System.Text;
using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class PingInfo : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.PingInfo;
        public override bool IsReliable => true;

        public PositioningTypes PositioningType { get; set; }
        public int ConnectedParticipants { get; set; }
        public string MOTD { get; set; } = string.Empty;

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            PositioningType = (PositioningTypes)dataStream[offset]; //Read Positioning Type - 1 byte.
            offset++;

            ConnectedParticipants = BitConverter.ToInt32(dataStream, offset); //Read Connected Participants - 4 bytes.
            offset += sizeof(int);

            var motdLength = BitConverter.ToInt32(dataStream, offset); //Read MOTD Length - 4 bytes.
            offset += sizeof(int);

            if(motdLength > 0)
                MOTD = Encoding.UTF8.GetString(dataStream, offset, motdLength); //Read MOTD.

            offset += motdLength;
            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            dataStream.Add((byte)PositioningType);
            dataStream.AddRange(BitConverter.GetBytes(ConnectedParticipants));
            dataStream.AddRange(BitConverter.GetBytes(MOTD.Length));
            if (MOTD.Length > 0)
                dataStream.AddRange(Encoding.UTF8.GetBytes(MOTD));
        }
    }
}
