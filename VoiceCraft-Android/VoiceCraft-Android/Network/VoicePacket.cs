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

    public enum StateIdentifier
    {
        Unknown,
        Mute,
        Unmute,
        Deafen,
        Undeafen
    }
    public class VoicePacket
    {
        private PacketIdentifier PacketIdentifier; //PacketIdentifier - Data containing data to identify what packet is received/sent
        private StateIdentifier StateIdentifier; //StateIdentifier - Data containing data to identify the state.
        private float Volume; //Volume - Data containing volume data to set for proximity.
        private string LoginKey; //LoginKey - Data containing the login Id. Used only for binding to a client connected to signalling server.
        private string Version; //Version - Data containing the packet Version/VoiceCraft Version. Needed 
        private byte[] Audio; //Audio - Data containing audio data.

        public PacketIdentifier PacketDataIdentifier
        {
            get { return PacketIdentifier; }
            set { PacketIdentifier = value; }
        }

        public StateIdentifier PacketStateIdentifier
        {
            get { return StateIdentifier; }
            set { StateIdentifier = value; }
        }

        public float PacketVolume
        {
            get { return Volume; }
            set { Volume = value; }
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
            StateIdentifier = StateIdentifier.Unknown;
            LoginKey = "";
            Volume = 0.0f;
            Audio = null;
        }

        public VoicePacket(byte[] dataStream)
        {
            PacketDataIdentifier = (PacketIdentifier)BitConverter.ToInt32(dataStream, 0); //Read packet identifier - 4 bytes.
            PacketStateIdentifier = (StateIdentifier)BitConverter.ToInt32(dataStream, 4); //Read packet state - 4 bytes.
            Volume = BitConverter.ToSingle(dataStream, 8); //Read volume value - 4 bytes.
            int loginIdLength = BitConverter.ToInt32(dataStream, 12); // Read login Id Length - 4 bytes.
            int versionLength = BitConverter.ToInt32(dataStream, 16); //Read Version Length - 4 bytes.
            int audioLength = BitConverter.ToInt32(dataStream, 20); //Read audio data length - 4 bytes.
            Audio = new byte[audioLength];

            if (loginIdLength > 0)
                LoginKey = Encoding.UTF8.GetString(dataStream, 24, loginIdLength);
            else
                LoginKey = null;

            if (versionLength > 0)
                Version = Encoding.UTF8.GetString(dataStream, 24 + loginIdLength, versionLength);
            else
                Version = null;

            if (audioLength > 0)
                Buffer.BlockCopy(dataStream, 24 + versionLength + loginIdLength, Audio, 0, audioLength);
            else
                Audio = null;
        }

        public byte[] GetPacketDataStream()
        {
            var DataStream = new List<byte>();
            DataStream.AddRange(BitConverter.GetBytes((int)PacketDataIdentifier));
            DataStream.AddRange(BitConverter.GetBytes((int)PacketStateIdentifier));
            DataStream.AddRange(BitConverter.GetBytes(PacketVolume));

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
