using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetChannels : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.GetChannels;
        public List<Channel> Channels { get; set; } = new List<Channel>(); //FIX THIS
    }
}
