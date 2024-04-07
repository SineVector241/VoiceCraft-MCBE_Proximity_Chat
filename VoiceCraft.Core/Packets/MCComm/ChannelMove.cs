namespace VoiceCraft.Core.Packets.MCComm
{
    public class ChannelMove : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.ChannelMove;
        public string PlayerId { get; set; } = string.Empty;
        public int ChannelId { get; set; } = 0; //Set to -1 to disconnect the participant from the channel.
    }
}
