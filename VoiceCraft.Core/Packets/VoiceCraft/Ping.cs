namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class Ping : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.Ping;
        public override bool IsReliable => false;
    }
}
