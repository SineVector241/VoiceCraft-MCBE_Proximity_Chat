using System.Collections.Generic;
using System;
using System.Text;

namespace VoiceCraft_Mobile.Utils
{
    public enum PacketIdentifier
    {
        Login,
        Logout,
        Accept,
        Deny,
        Ready,
        Ping,
        AudioStream,
        Null
    }

    public class Packet
    {
        //Packet Data
        private PacketIdentifier packetIdentifier; //PacketIdentifier - Data containing data to identify what packet is received/sent
        private float Volume; //Volume - Data containing volume info when server sends AudioStream Packet to client
        private string? Name; //Name - Data containing each players Ingame Name.
        private string? SessionKey; //Session key - Data containing the key which identifies the user's session.
        private byte[]? AudioBuffer; //Microphone Audio - Data containing audio data to either forward or play.

        public PacketIdentifier VCPacketDataIdentifier
        {
            get { return packetIdentifier; }
            set { packetIdentifier = value; }
        }

        public float VCVolume
        {
            get { return Volume; }
            set { Volume = value; }
        }

        public string VCName
        {
            get { return Name; }
            set { Name = value; }
        }

        public string VCSessionKey
        {
            get { return SessionKey; }
            set { SessionKey = value; }
        }

        public byte[] VCAudioBuffer
        {
            get { return AudioBuffer; }
            set { AudioBuffer = value; }
        }

        public Packet()
        {
            VCPacketDataIdentifier = PacketIdentifier.Null;
            Volume = 0.0f;
            VCName = null;
            VCSessionKey = null;
            VCAudioBuffer = null;
        }

        public Packet(byte[] dataStream)
        {
            AudioBuffer = new byte[400];
            VCPacketDataIdentifier = (PacketIdentifier)BitConverter.ToInt32(dataStream, 0); //Read packet identifier - 4 bytes.
            VCVolume = BitConverter.ToSingle(dataStream, 4); //Read Before Volume - 4 bytes.
            int usernameLength = BitConverter.ToInt32(dataStream, 8); //Read username length - 4 bytes.
            int sessionKeyLength = BitConverter.ToInt32(dataStream, 12); //Read session key length - 4 bytes.
            int audioBufferLength = BitConverter.ToInt32(dataStream, 16); //Read audio data length - 4 bytes.

            if (usernameLength > 0)
                Name = Encoding.UTF8.GetString(dataStream, 20, usernameLength);
            else
                Name = null;

            if (sessionKeyLength > 0)
                SessionKey = Encoding.UTF8.GetString(dataStream, 20 + usernameLength, sessionKeyLength);
            else
                SessionKey = null;

            if (audioBufferLength > 0)
                Buffer.BlockCopy(dataStream, 20 + usernameLength + sessionKeyLength, AudioBuffer, 0, audioBufferLength);
            else
                AudioBuffer = null;
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();

            DataStream.AddRange(BitConverter.GetBytes((int)VCPacketDataIdentifier));
            DataStream.AddRange(BitConverter.GetBytes((float)VCVolume));

            if (Name != null)
                DataStream.AddRange(BitConverter.GetBytes(Name.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (SessionKey != null)
                DataStream.AddRange(BitConverter.GetBytes(SessionKey.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (AudioBuffer != null)
                DataStream.AddRange(BitConverter.GetBytes(AudioBuffer.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));


            if (Name != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(Name));

            if (SessionKey != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(SessionKey));

            if (AudioBuffer != null)
                DataStream.AddRange(AudioBuffer);

            return DataStream.ToArray();
        }
    }
}
