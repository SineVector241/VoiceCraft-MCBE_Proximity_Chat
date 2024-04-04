namespace VoiceCraft.Core.Packets.MCComm
{
    public class ChannelMove : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketId.ChannelMove;
        public string PlayerId { get; set; } = string.Empty;
        public byte ChannelId { get; set; } = 0; //Set to 0 to disconnect the participant from the channel.
    }
}
