using System.Text;
using VoiceCraft.Server.Helpers;

namespace VoiceCraft.Server.Network.Packets
{
    public enum SignallingPacketIdentifiers
    {
        LoginServerSided,
        LoginClientSided,
        Login,
        Logout,
        Accept16,
        Accept48,
        Deny,
        Binded,
        Error,
        Ping,
        InfoPing,
        Null
    }

    public class SignallingPacket
    {
        private SignallingPacketIdentifiers Identifier; //Data containing the identifier of the packet.
        private AudioCodecs Codec; //Data containing the type of audio codec to use.
        private ushort Key; //Data containing the login key to bind the player and the incoming login.
        private ushort VoicePort; //Data containing the voice port.
        private string? Version; //Data containing the version of the packet.
        private string? Metadata; //Data containing string metadata.

        public SignallingPacketIdentifiers PacketIdentifier
        {
            get { return Identifier; }
            set { Identifier = value; }
        }

        public AudioCodecs PacketCodec
        {
            get { return Codec; }
            set { Codec = value; }
        }

        public ushort PacketKey
        {
            get { return Key; }
            set { Key = value; }
        }

        public ushort PacketVoicePort
        {
            get { return VoicePort; }
            set { VoicePort = value; }
        }

        public string? PacketVersion
        {
            get { return Version; }
            set { Version = value; }
        }

        public string? PacketMetadata
        {
            get { return Metadata; }
            set { Metadata = value; }
        }

        public SignallingPacket()
        {
            PacketIdentifier = SignallingPacketIdentifiers.Null;
            PacketCodec = AudioCodecs.Opus;
            PacketKey = 0;
            PacketVoicePort = 0;
            PacketVersion = string.Empty;
            PacketMetadata = string.Empty;
        }

        public SignallingPacket(byte[] DataStream)
        {
            PacketIdentifier = (SignallingPacketIdentifiers)BitConverter.ToUInt16(DataStream, 0); //Read packet identifier - 2 bytes.
            PacketCodec = (AudioCodecs)BitConverter.ToUInt16(DataStream, 2); //Read packet codec - 2 bytes.
            PacketKey = BitConverter.ToUInt16(DataStream, 4); //Read packet key - 2 bytes.
            PacketVoicePort = BitConverter.ToUInt16(DataStream, 6); //Read packet voice port - 2 bytes.

            //String lengths
            int versionLength = BitConverter.ToInt32(DataStream, 8); //Read version length - 4 bytes.
            int metadataLength = DataStream.Length - (12 + versionLength);

            if (versionLength > 0)
                PacketVersion = Encoding.UTF8.GetString(DataStream, 12, versionLength);
            else
                PacketVersion = string.Empty;

            if (metadataLength > 0)
                PacketMetadata = Encoding.UTF8.GetString(DataStream, 12 + versionLength, metadataLength);
            else
                PacketMetadata = string.Empty;
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();

            DataStream.AddRange(BitConverter.GetBytes((ushort)Identifier)); //Packet Identifier
            DataStream.AddRange(BitConverter.GetBytes((ushort)Codec)); //Packet Codec
            DataStream.AddRange(BitConverter.GetBytes(Key)); //Packet Key
            DataStream.AddRange(BitConverter.GetBytes(VoicePort)); //Packet Voice Port

            //String Values
            if (!string.IsNullOrWhiteSpace(Version))
                DataStream.AddRange(BitConverter.GetBytes(Version.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if(!string.IsNullOrWhiteSpace(Version))
                DataStream.AddRange(Encoding.UTF8.GetBytes(Version));

            if (!string.IsNullOrWhiteSpace(Metadata))
                DataStream.AddRange(Encoding.UTF8.GetBytes(Metadata));

            return DataStream.ToArray();
        }
    }
}
