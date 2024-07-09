namespace VoiceCraft.Core.Packets.MCComm
{
    public class GetDefaultSettings : MCCommPacket
    {
        public override byte PacketId => (byte)MCCommPacketTypes.GetDefaultSettings;
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = false;
        public bool VoiceEffects { get; set; } = false;
    }
}
