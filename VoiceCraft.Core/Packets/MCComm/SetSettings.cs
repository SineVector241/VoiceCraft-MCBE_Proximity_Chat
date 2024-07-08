namespace VoiceCraft.Core.Packets.MCComm
{
    public class SetSettings : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.SetSettings;
        public byte ChannelId { get; set; }
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = false;
        public bool VoiceEffects { get; set; } = false;
    }
}
