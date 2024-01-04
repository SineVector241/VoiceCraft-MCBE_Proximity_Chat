using VoiceCraft.Core.Packets.Interfaces;

namespace VoiceCraft.Core.Packets.MCComm
{
    public class UpdateSettings : IMCCommPacketData
    {
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = false;
        public bool VoiceEffects { get; set; } = false;

        public static MCCommPacket Create(int proximityDistance, bool proximityToggle, bool voiceEffects)
        {
            return new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.UpdateSettings,
                PacketData = new UpdateSettings()
                {
                    ProximityDistance = proximityDistance,
                    ProximityToggle = proximityToggle,
                    VoiceEffects = voiceEffects
                }
            };
        }
    }
}
