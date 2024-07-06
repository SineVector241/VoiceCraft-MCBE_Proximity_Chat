namespace VoiceCraft.Core.Packets.MCComm
{
    public class ChannelRemove : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.ChannelRemove;
        public string PlayerId { get; set; } = string.Empty;
        public byte ChannelId { get; set; } = 0; //0 For Main. If participant is removed from another channel, they will automatically be added to main. you cannot remove participants that are only in main.
    }
}
