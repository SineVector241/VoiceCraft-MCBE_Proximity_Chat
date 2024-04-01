namespace VoiceCraft.Network.Packets
{
    public abstract class VoiceCraftPacket
    {
        public abstract byte PacketId { get; }
        public abstract bool IsReliable { get; }
        internal uint Sequence { get; set; }
        internal long ResendTime { get; set; }

        public VoiceCraftPacket() { }

        public virtual int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            if (IsReliable)
            {
                Sequence = BitConverter.ToUInt32(dataStream, offset);
                offset += sizeof(uint);
            }

            return offset; //Returns the amount of data read.
        }

        public virtual void WritePacket(ref List<byte> dataStream)
        {
            dataStream.Clear(); //Clear the packet stream.
            dataStream.Add(PacketId);
            if(IsReliable)
                dataStream.AddRange(BitConverter.GetBytes(Sequence));
        }
    }

    public enum VoiceCraftPacketTypes : byte
    {
        //SYSTEM/PROTOCOL PACKETS
        Login,
        Logout,
        Accept,
        Deny,
        Ack,
        Ping,
        PingInfo,

        //USER PACKETS
        ParticipantJoined,
        ParticipantLeft,
        Mute,
        Unmute,
        Deafen,
        Undeafen,
        JoinChannel,
        LeaveChannel,
        AddChannel,
        RemoveChannel,

        //VOICE PACKETS
        UpdatePosition,
        ClientAudio,
        ServerAudio
    }
}