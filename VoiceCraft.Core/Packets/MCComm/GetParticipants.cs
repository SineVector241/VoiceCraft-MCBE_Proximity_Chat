using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetParticipants : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.GetParticipants;
        public List<string> Players { get; set; } = new List<string>();
    }
}
