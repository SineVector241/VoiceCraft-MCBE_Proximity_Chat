using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class AckUpdate : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketId.AckUpdate;

        public List<string> SpeakingPlayers = new List<string>();
    }
}
