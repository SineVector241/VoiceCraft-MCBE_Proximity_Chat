namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class Binded : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Binded;
        public override bool IsReliable => true;

        public string Name { get; set; }
    }
}
