namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class LeaveChannel : VoiceCraftPacket
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.LeaveChannel;
        public override bool IsReliable => true;
    }
}