namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class Unbinded : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Unbinded;
        public override bool IsReliable => true;
    }
}
