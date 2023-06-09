using System;
using System.Collections.Generic;
using System.Text;
using VoiceCraft.Mobile.Network.Codecs;

namespace VoiceCraft.Mobile.Network.Packets
{
    public enum SignallingPacketIdentifiers
    {
        LoginServerSided,
        LoginClientSided,
        Logout,
        Accept16,
        Accept48,
        Deny,
        Binded,
        Error,
        Null
    }

    public class SignallingPacket
    {
        private SignallingPacketIdentifiers Identifier; //Data containing the identifier of the packet.
        private AudioCodecs Codec; //Data containing the type of audio codec to use.
        private ushort Key; //Data containing the login key to bind the player and the incoming login.
        private ushort VoicePort; //Data containing the voice port.
        private string Version; //Data containing the version of the packet.
        private string Metadata; //Data containing string metadata.

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

        public string PacketVersion
        {
            get { return Version; }
            set { Version = value; }
        }

        public string PacketMetadata
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
            PacketIdentifier = (SignallingPacketIdentifiers)DataStream[0]; //Read packet identifier - 1 byte.
            PacketCodec = (AudioCodecs)DataStream[1]; //Read packet codec - 1 byte.
            PacketKey = BitConverter.ToUInt16(DataStream, 2); //Read packet key - 2 bytes.
            PacketVoicePort = BitConverter.ToUInt16(DataStream, 2); //Read packet voice port - 2 bytes.

            //String lengths
            int versionLength = BitConverter.ToInt32(DataStream, 6); //Read version length - 4 bytes.
            int metadataLength = DataStream.Length - (10 + versionLength);

            if (versionLength > 0)
                PacketVersion = Encoding.UTF8.GetString(DataStream, 10, versionLength);
            else
                PacketVersion = string.Empty;

            if (metadataLength > 0)
                PacketMetadata = Encoding.UTF8.GetString(DataStream, 10 + versionLength, metadataLength);
            else
                PacketMetadata = string.Empty;
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();

            DataStream.AddRange(BitConverter.GetBytes((byte)Identifier)); //Packet Identifier
            DataStream.AddRange(BitConverter.GetBytes((byte)Codec)); //Packet Codec
            DataStream.AddRange(BitConverter.GetBytes(Key)); //Packet Key
            DataStream.AddRange(BitConverter.GetBytes(VoicePort)); //Packet Voice Port

            //String Values
            if (!string.IsNullOrEmpty(Version))
                DataStream.AddRange(BitConverter.GetBytes(Version.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if(!string.IsNullOrEmpty(Version))
                DataStream.AddRange(Encoding.UTF8.GetBytes(Version));

            if (!string.IsNullOrEmpty(Metadata))
                DataStream.AddRange(Encoding.UTF8.GetBytes(Metadata));

            return DataStream.ToArray();
        }
    }
}
