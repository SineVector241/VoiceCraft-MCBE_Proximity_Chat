using System.Collections.Generic;
using System;
using System.Numerics;
using System.Text;

namespace VoiceCraft_Server
{
    public enum PacketIdentifier
    {
        Login,
        Logout,
        Accept,
        Deny,
        Ping,
        Null
    }

    public class Packet
    {
        //Packet Data
        private PacketIdentifier packetIdentifier; //PacketIdentifier - Data containing data to identify what packet is received/sent
        private string? Version; //Version - Data containing the version of voicecraft being used.
        private string? LoginId; //LoginId - Data containing the login key to bind the player and the incoming login.
        private string? Name; //Name - Data containing the players Ingame Name.
        private string? STUN; //STUN - Data containing the Stun Server's address and other things. Not sure as of yet.
        private Vector3 Position; //Position - Data containing the players position.

        public PacketIdentifier PacketDataIdentifier
        {
            get { return packetIdentifier; }
            set { packetIdentifier = value; }
        }

        public string PacketVersion
        {
            get { return Version; }
            set { Version = value; }
        }

        public string PacketLoginId
        {
            get { return LoginId; }
            set { LoginId = value; }
        }

        public string PacketName
        {
            get { return Name; }
            set { Name = value; }
        }

        public string PacketSTUN
        {
            get { return STUN; }
            set { STUN = value; }
        }

        public Vector3 PacketPosition
        {
            get { return Position; }
            set { Position = value; }
        }

        public Packet()
        {
            PacketDataIdentifier = PacketIdentifier.Null;
            PacketVersion = "";
            PacketLoginId = "";
            PacketName = "";
            PacketPosition = new Vector3();
        }

        public Packet(byte[] dataStream)
        {
            PacketDataIdentifier = (PacketIdentifier)BitConverter.ToInt32(dataStream, 0); //Read packet identifier - 4 bytes.
            int versionLength = BitConverter.ToInt32(dataStream, 4); //Read packet version length - 4 bytes.
            int loginIdLength = BitConverter.ToInt32(dataStream, 8); //Read packet loginId Length - 4 bytes.
            int usernameLength = BitConverter.ToInt32(dataStream, 12); //Read username length - 4 bytes.
            int stunLength = BitConverter.ToInt32(dataStream, 16); //Read STUN length - 4 bytes.
            float positionX = BitConverter.ToSingle(dataStream, 20); //Read X float position - 4 bytes.
            float positionY = BitConverter.ToSingle(dataStream, 24); //Read Y float position - 4 bytes.
            float positionZ = BitConverter.ToSingle(dataStream, 28); //Read Z float position - 4 bytes.

            if (versionLength > 0)
                Version = Encoding.UTF8.GetString(dataStream, 32, versionLength);
            else
                Version = null;

            if (loginIdLength > 0)
                LoginId = Encoding.UTF8.GetString(dataStream, 32 + versionLength, loginIdLength);
            else
                LoginId = null;

            if (usernameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, 32 + loginIdLength + versionLength, usernameLength);
            else
                Name = null;

            if (stunLength > 0)
                STUN = Encoding.UTF8.GetString(dataStream, 32 + usernameLength + loginIdLength + versionLength, stunLength);
            else
                STUN = null;

            //Create new Vector3 and write to Position variable;
            Position = new Vector3(positionX, positionY, positionZ);
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();

            DataStream.AddRange(BitConverter.GetBytes((int)PacketDataIdentifier));
            DataStream.AddRange(BitConverter.GetBytes(PacketPosition.X));
            DataStream.AddRange(BitConverter.GetBytes(PacketPosition.Y));
            DataStream.AddRange(BitConverter.GetBytes(PacketPosition.Z));

            if (Version != null)
                DataStream.AddRange(BitConverter.GetBytes(Version.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (LoginId != null)
                DataStream.AddRange(BitConverter.GetBytes(LoginId.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (Name != null)
                DataStream.AddRange(BitConverter.GetBytes(Name.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (STUN != null)
                DataStream.AddRange(BitConverter.GetBytes(STUN.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));


            if (Version != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(Version));

            if (LoginId != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(LoginId));

            if (Name != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(Name));

            if (STUN != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(STUN));

            return DataStream.ToArray();
        }
    }
}