using System.Collections.Generic;
using System;
using System.Numerics;
using System.Text;

namespace Packet
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
        private string? Name; //Name - Data containing the players Ingame Name.
        private string? STUN; //STUN - Data containing the Stun Server's address and other things. Not sure as of yet.
        private Vector3 Position; //Position - Data containing the players position.

        public PacketIdentifier PacketDataIdentifier
        {
            get { return packetIdentifier; }
            set { packetIdentifier = value; }
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
            PacketName = "";
            PacketPosition = new Vector3();
        }

        public Packet(byte[] dataStream)
        {
            PacketDataIdentifier = (PacketIdentifier)BitConverter.ToInt32(dataStream, 0); //Read packet identifier - 4 bytes.
            int usernameLength = BitConverter.ToInt32(dataStream, 4); //Read username length - 4 bytes.
            int stunLength = BitConverter.ToInt32(dataStream, 8); //Read STUN length - 4 bytes.
            float positionX = BitConverter.ToSingle(dataStream, 12); //Read X float position - 4 bytes.
            float positionY = BitConverter.ToSingle(dataStream, 16); //Read Y float position - 4 bytes.
            float positionZ = BitConverter.ToSingle(dataStream, 20); //Read Z float position - 4 bytes.

            if (usernameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, 24, usernameLength);
            else
                Name = null;

            if (stunLength > 0)
                STUN = Encoding.UTF8.GetString(dataStream, 24 + usernameLength, stunLength);
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

            if (Name != null)
                DataStream.AddRange(BitConverter.GetBytes(Name.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (STUN != null)
                DataStream.AddRange(BitConverter.GetBytes(STUN.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));


            if (Name != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(Name));

            if (STUN != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(STUN));

            return DataStream.ToArray();
        }
    }
}