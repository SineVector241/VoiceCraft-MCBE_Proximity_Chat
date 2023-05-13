using System;
using System.Collections.Generic;
using System.Text;

namespace VCVoice_Packet
{
    public enum PacketIdentifier
    {
        Login,
        Logout,
        Accept,
        Deny,
        Audio,
        StateChanged,
        Null
    }

    public class VoicePacket
    {
        private PacketIdentifier PacketIdentifier; //PacketIdentifier - Data containing data to identify what packet is received/sent
        private float RotationSource; //RotationSource - Data containing rotation to source.
        private float Volume; //Volume - Data containing volume data to set for proximity.
        private int BytesRecorded; //Bytes Recorded - Data containing how many bytes were recorded in the packet.
        private string LoginKey; //LoginKey - Data containing the login Id. Used only for binding to a client connected to signalling server.
        private string Version; //Version - Data containing the packet Version/VoiceCraft Version. Needed 
        private byte[] Audio; //Audio - Data containing audio data.

        public PacketIdentifier PacketDataIdentifier
        {
            get { return PacketIdentifier; }
            set { PacketIdentifier = value; }
        }

        public float PacketRotationSource
        {
            get { return RotationSource; }
            set { RotationSource = value; }
        }

        public float PacketVolume
        {
            get { return Volume; }
            set { Volume = value; }
        }

        public int PacketBytesRecorded
        {
            get { return BytesRecorded; }
            set { BytesRecorded = value; }
        }

        public string PacketLoginKey
        {
            get { return LoginKey; }
            set { LoginKey = value; }
        }

        public string PacketVersion
        {
            get { return Version; }
            set { Version = value; }
        }

        public byte[] PacketAudio
        {
            get { return Audio; }
            set { Audio = value; }
        }

        public VoicePacket()
        {
            PacketIdentifier = PacketIdentifier.Null;
            LoginKey = "";
            Volume = 0.0f;
            Audio = null;
            BytesRecorded = 0;
        }

        public VoicePacket(byte[] dataStream)
        {
            PacketDataIdentifier = (PacketIdentifier)BitConverter.ToInt32(dataStream, 0); //Read packet identifier - 4 bytes.
            PacketRotationSource = BitConverter.ToSingle(dataStream, 4); //Read packet rotation source - 4 bytes.
            Volume = BitConverter.ToSingle(dataStream, 8); //Read volume value - 4 bytes.
            BytesRecorded = BitConverter.ToInt32(dataStream, 12); //Read Bytes Recorded value - 4 bytes.
            int loginIdLength = BitConverter.ToInt32(dataStream, 16); // Read login Id Length - 4 bytes.
            int versionLength = BitConverter.ToInt32(dataStream, 20); //Read Version Length - 4 bytes.
            int audioLength = BitConverter.ToInt32(dataStream, 24); //Read audio data length - 4 bytes.
            Audio = new byte[audioLength];

            if (loginIdLength > 0)
                LoginKey = Encoding.UTF8.GetString(dataStream, 28, loginIdLength);
            else
                LoginKey = null;

            if (versionLength > 0)
                Version = Encoding.UTF8.GetString(dataStream, 28 + loginIdLength, versionLength);
            else
                Version = null;

            if (audioLength > 0)
                Buffer.BlockCopy(dataStream, 28 + versionLength + loginIdLength, Audio, 0, audioLength);
            else
                Audio = null;
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();
            DataStream.AddRange(BitConverter.GetBytes((int)PacketDataIdentifier));
            DataStream.AddRange(BitConverter.GetBytes(PacketRotationSource));
            DataStream.AddRange(BitConverter.GetBytes(PacketVolume));
            DataStream.AddRange(BitConverter.GetBytes(PacketBytesRecorded));

            if (PacketLoginKey != null)
                DataStream.AddRange(BitConverter.GetBytes(PacketLoginKey.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (PacketVersion != null)
                DataStream.AddRange(BitConverter.GetBytes(PacketVersion.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));

            if (PacketAudio != null)
                DataStream.AddRange(BitConverter.GetBytes(PacketAudio.Length));
            else
                DataStream.AddRange(BitConverter.GetBytes(0));


            if (PacketLoginKey != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(PacketLoginKey));

            if (PacketVersion != null)
                DataStream.AddRange(Encoding.UTF8.GetBytes(PacketVersion));

            if (PacketAudio != null)
                DataStream.AddRange(PacketAudio);

            return DataStream.ToArray();
        }
    }
}