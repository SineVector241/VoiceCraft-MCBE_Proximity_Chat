using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceCraft.Mobile.Network.Packets
{
    public enum SignallingPacketIdentifiers
    {
        LoginServerSided,
        LoginClientSided,
        Login,
        Logout,
        Accept,
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
        private ushort Key; //Data containing the login key to bind the player and the incoming login.
        private ushort VoicePort; //Data containing the voice port.
        private string? Version; //Data containing the version of the packet.
        private string? Metadata; //Data containing string metadata.

        public SignallingPacketIdentifiers PacketIdentifier
        {
            get { return Identifier; }
            set { Identifier = value; }
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
            PacketKey = 0;
            PacketVoicePort = 0;
            PacketVersion = string.Empty;
            PacketMetadata = string.Empty;
        }

        public SignallingPacket(byte[] DataStream)
        {
            PacketIdentifier = (SignallingPacketIdentifiers)BitConverter.ToUInt16(DataStream, 0); //Read packet identifier - 2 bytes.
            PacketKey = BitConverter.ToUInt16(DataStream, 2); //Read packet key - 2 bytes.
            PacketVoicePort = BitConverter.ToUInt16(DataStream, 4); //Read packet voice port - 2 bytes.

            //String lengths
            int versionLength = BitConverter.ToInt32(DataStream, 6); //Read version length - 4 bytes.
            int metadataLength = BitConverter.ToInt32(DataStream, 10); //Read Metadata Length - 4 bytes.

            if (versionLength > 0)
                PacketVersion = Encoding.UTF8.GetString(DataStream, 14, versionLength);
            else
                PacketVersion = string.Empty;

            if (metadataLength > 0)
                PacketMetadata = Encoding.UTF8.GetString(DataStream, 14 + versionLength, metadataLength);
            else
                PacketMetadata = string.Empty;
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();

            DataStream.AddRange(BitConverter.GetBytes((ushort)Identifier)); //Packet Identifier
            DataStream.AddRange(BitConverter.GetBytes(Key)); //Packet Key
            DataStream.AddRange(BitConverter.GetBytes(VoicePort)); //Packet Voice Port

            //String Values
            if (!string.IsNullOrWhiteSpace(Version))
                DataStream.AddRange(BitConverter.GetBytes(Version.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (!string.IsNullOrWhiteSpace(Metadata))
                DataStream.AddRange(BitConverter.GetBytes(Metadata.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));


            if (!string.IsNullOrWhiteSpace(Version))
                DataStream.AddRange(Encoding.UTF8.GetBytes(Version));

            if (!string.IsNullOrWhiteSpace(Metadata))
                DataStream.AddRange(Encoding.UTF8.GetBytes(Metadata));

            return DataStream.ToArray();
        }
    }
}