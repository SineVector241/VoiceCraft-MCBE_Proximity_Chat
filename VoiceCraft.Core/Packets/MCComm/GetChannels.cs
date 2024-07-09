using System.Collections.Generic;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetChannels : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.GetChannels;
        public Dictionary<ushort, Channel> Channels { get; set; } = new Dictionary<ushort, Channel>();
    }
}
