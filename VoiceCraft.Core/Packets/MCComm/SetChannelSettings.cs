namespace VoiceCraft.Core.Packets.MCComm
{
    public class SetChannelSettings : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.SetChannelSettings;
        public byte ChannelId { get; set; }
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = false;
        public bool VoiceEffects { get; set; } = false;
    }
}
