using System.Collections.Generic;
using System;
using System.Text;

namespace VCSignalling_Packet
{
    public enum PacketIdentifier
    {
        Login,
        Logout,
        Accept,
        Deny,
        Ping,
        Binded,
        Null
    }

    public class SignallingPacket
    {
        //Packet Data
        private PacketIdentifier packetIdentifier; //PacketIdentifier - Data containing data to identify what packet is received/sent
        private int VoicePort; //VoicePort - Data containing the port of the Voice server;
        private string Version; //Version - Data containing the version of voicecraft being used.
        private string LoginKey; //LoginKey - Data containing the login key to bind the player and the incoming login.
        private string Name; //Name - Data containing the players Ingame Name.

        public PacketIdentifier PacketDataIdentifier
        {
            get { return packetIdentifier; }
            set { packetIdentifier = value; }
        }

        public int PacketVoicePort
        {
            get { return VoicePort; }
            set { VoicePort = value; }
        }

        public string PacketVersion
        {
            get { return Version; }
            set { Version = value; }
        }

        public string PacketLoginKey
        {
            get { return LoginKey; }
            set { LoginKey = value; }
        }

        public string PacketName
        {
            get { return Name; }
            set { Name = value; }
        }

        public SignallingPacket()
        {
            PacketDataIdentifier = PacketIdentifier.Null;
            PacketVoicePort = 0;
            PacketVersion = "";
            PacketLoginKey = "";
            PacketName = "";
        }

        public SignallingPacket(byte[] dataStream)
        {
            PacketDataIdentifier = (PacketIdentifier)BitConverter.ToInt32(dataStream, 0); //Read packet identifier - 4 bytes.
            //Integer Values
            VoicePort = BitConverter.ToInt32(dataStream, 4); //Read VoicePort - 4 bytes.

            //String Lengths
            int versionLength = BitConverter.ToInt32(dataStream, 8); //Read packet version length - 4 bytes.
            int loginIdLength = BitConverter.ToInt32(dataStream, 12); //Read packet loginId Length - 4 bytes.
            int usernameLength = BitConverter.ToInt32(dataStream, 16); //Read username length - 4 bytes.

            if (versionLength > 0)
                Version = Encoding.UTF8.GetString(dataStream, 20, versionLength);
            else
                Version = null;

            if (loginIdLength > 0)
                LoginKey = Encoding.UTF8.GetString(dataStream, 20 + versionLength, loginIdLength);
            else
                LoginKey = null;

            if (usernameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, 20 + loginIdLength + versionLength, usernameLength);
            else
                Name = null;
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();

            //Packet Identifier
            DataStream.AddRange(BitConverter.GetBytes((int)PacketDataIdentifier));

            //Voice Port Value
            DataStream.AddRange(BitConverter.GetBytes(PacketVoicePort));

            //String Values
            if (Version != null)
                DataStream.AddRange(BitConverter.GetBytes(Version.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (LoginKey != null)
                DataStream.AddRange(BitConverter.GetBytes(LoginKey.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (Name != null)
                DataStream.AddRange(BitConverter.GetBytes(Name.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));


            if (Version != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(Version));

            if (LoginKey != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(LoginKey));

            if (Name != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(Name));

            return DataStream.ToArray();
        }
    }
}