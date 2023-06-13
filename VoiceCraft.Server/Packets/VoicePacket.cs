using System.Numerics;
using System.Text;

namespace VoiceCraft.Server.Network.Packets
{
    public enum VoicePacketIdentifier
    {
        Login,
        Logout,
        Accept,
        Deny,
        Audio,
        UpdatePosition,
        Error,
        Null
    }

    public class VoicePacket
    {
        private VoicePacketIdentifier Identifier; //Data containing the packet identifier.
        private uint Count; //Data containing packet count to detect packet loss.
        private ushort Key; //Data containing the key of a participant.
        private ushort Distance; //Data containing the distance to calculate the volume level.
        private Vector3 Position; //Data containing audio source assuming client audio handling is at 0,0,0 rotation 0.
        private string? EnviromentId; //Data containing the server and dimension the player is in. (Client Sided positioning only)
        private byte[]? Audio; //Data containing encoded audio data.

        public VoicePacketIdentifier PacketIdentifier
        {
            get { return Identifier; }
            set { Identifier = value; }
        }

        public uint PacketCount
        {
            get { return Count; }
            set { Count = value; }
        }

        public ushort PacketKey
        {
            get { return Key; }
            set { Key = value; }
        }

        public ushort PacketDistance
        {
            get { return Distance; }
            set { Distance = value; }
        }

        public Vector3 PacketPosition
        {
            get { return Position; }
            set { Position = value; }
        }

        public string? PacketEnviromentId
        {
            get { return EnviromentId; }
            set { EnviromentId = value; }
        }

        public byte[]? PacketAudio
        {
            get { return Audio; }
            set { Audio = value; }
        }

        public VoicePacket()
        {
            PacketIdentifier = VoicePacketIdentifier.Null;
            PacketCount = 0;
            PacketKey = 0;
            PacketDistance = 0;
            PacketPosition = new Vector3();
            PacketEnviromentId = string.Empty;
            PacketAudio = Array.Empty<byte>();
        }

        public VoicePacket(byte[] DataStream)
        {
            PacketIdentifier = (VoicePacketIdentifier)BitConverter.ToUInt16(DataStream, 0); //Read packet identifier - 2 bytes.
            PacketCount = BitConverter.ToUInt32(DataStream, 2); //Read packet count - 4 bytes.
            PacketKey = BitConverter.ToUInt16(DataStream, 6); //Read packet key - 2 bytes.
            PacketDistance = BitConverter.ToUInt16(DataStream, 8); //Read packet distance - 2 bytes.
            PacketPosition = new Vector3(BitConverter.ToSingle(DataStream, 10), BitConverter.ToSingle(DataStream, 14), BitConverter.ToSingle(DataStream, 18)); //Read packet position 12 bytes.
            
            //String lengths.
            int enviromentIdLength = BitConverter.ToInt32(DataStream, 22); //Read enviroment id length - 4 bytes.
            int audioLength = DataStream.Length - (26 + enviromentIdLength); //Read audio length.
            PacketAudio = new byte[audioLength];

            if (enviromentIdLength > 0)
                PacketEnviromentId = Encoding.UTF8.GetString(DataStream, 26, enviromentIdLength);
            else
                PacketEnviromentId = string.Empty;

            if (audioLength > 0)
                Buffer.BlockCopy(DataStream, 26 + enviromentIdLength, PacketAudio, 0, audioLength);
            else
                PacketAudio = Array.Empty<byte>();
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();

            DataStream.AddRange(BitConverter.GetBytes((ushort)Identifier)); //Packet Identifier
            DataStream.AddRange(BitConverter.GetBytes(Count)); //Packet Count
            DataStream.AddRange(BitConverter.GetBytes(Key)); //Packet Key
            DataStream.AddRange(BitConverter.GetBytes(PacketDistance)); //Packet Distance
            DataStream.AddRange(BitConverter.GetBytes(Position.X)); //Packet Position X
            DataStream.AddRange(BitConverter.GetBytes(Position.Y)); //Packet Position Y
            DataStream.AddRange(BitConverter.GetBytes(Position.Z)); //Packet Position Z

            //String Values
            if (!string.IsNullOrWhiteSpace(EnviromentId))
                DataStream.AddRange(BitConverter.GetBytes(EnviromentId.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrWhiteSpace(EnviromentId))
                DataStream.AddRange(Encoding.UTF8.GetBytes(EnviromentId));

            if (Audio != null && Audio.Length > 0)
                DataStream.AddRange(Audio);

            return DataStream.ToArray();
        }
    }
}
