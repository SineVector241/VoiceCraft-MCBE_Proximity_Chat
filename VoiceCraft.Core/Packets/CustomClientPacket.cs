using System.Collections.Generic;

namespace VoiceCraft.Core.Packets
{
    public abstract class CustomClientPacket
    {
        public abstract byte PacketId { get; }

        public virtual int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            return offset; //Returns the amount of data read.
        }

        public virtual void WritePacket(ref List<byte> dataStream)
        {
            dataStream.Clear(); //Clear the packet stream.
            dataStream.Add(PacketId);
        }
    }

    public enum CustomClientTypes : byte
    {
        Login,
        Logout,
        Accept,
        Deny,
        Update
    }
}
