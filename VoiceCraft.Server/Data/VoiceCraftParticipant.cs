using System.Numerics;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Data
{
    public class VoiceCraftParticipant : Participant
    {
        public short Key { get; set; }
        public bool Binded { get; set; }
        public bool ClientSided { get; set; }
        public bool ServerMuted { get; set; }
        public bool ServerDeafened { get; set; }
        public Channel Channel { get; set; }

        //Minecraft Data
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public float CaveDensity { get; set; }
        public bool Dead
        {
            get => ((ChecksBitmask >> (int)ParticipantBitmaskMap.DataBitmask) & (uint)DataBitmask.Dead) != 0;
            set
            {
                if (value)
                {
                    ChecksBitmask |= (uint)DataBitmask.Dead << (int)ParticipantBitmaskMap.DataBitmask;
                }
                else
                {
                    ChecksBitmask &= ~((uint)DataBitmask.Dead << (int)ParticipantBitmaskMap.DataBitmask);
                }
            }
        }
        public bool InWater
        {
            get => ((ChecksBitmask >> (int)ParticipantBitmaskMap.DataBitmask) & (uint)DataBitmask.InWater) != 0;
            set
            {
                if (value)
                {
                    ChecksBitmask |= (uint)DataBitmask.InWater << (int)ParticipantBitmaskMap.DataBitmask;
                }
                else
                {
                    ChecksBitmask &= ~((uint)DataBitmask.InWater << (int)ParticipantBitmaskMap.DataBitmask);
                }
            }
        }
        public string EnvironmentId { get; set; } = string.Empty;
        public string MinecraftId { get; set; } = string.Empty;
        public uint ChecksBitmask { get; set; } = uint.MaxValue; //All bits are set. 1111 1111 1111 1111 1111 1111 1111 1111

        public VoiceCraftParticipant(string name, Channel channel) : base(name)
        {
            Channel = channel;
        }

        public static short GenerateKey()
        {
            return (short)Random.Shared.Next(short.MinValue + 1, short.MaxValue); //short.MinValue is used to specify no Key.
        }

        public bool TalkListenSettingsEnabled(params BitmaskSettings[] settings)
        {
            uint allSettings = 0;
            for(int i = 0; i < settings.Length; i++)
            {
                allSettings = allSettings | (uint)settings[i];
            }

            uint result = 0;
            if ((((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask1) & 1) | ((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask1) & 1)) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask1Settings) & allSettings;

            if ((((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask2) & 1) | ((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask2) & 1)) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask2Settings) & allSettings;

            if ((((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask3) & 1) | ((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask3) & 1)) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask3Settings) & allSettings;

            if ((((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask4) & 1) | ((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask4) & 1)) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask4Settings) & allSettings;

            if ((((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask5) & 1) | ((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask5) & 1)) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask5Settings) & allSettings;

            return result != 0;
        }

        public bool ListenSettingsEnabled(params BitmaskSettings[] settings)
        {
            uint allSettings = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                allSettings = allSettings | (uint)settings[i];
            }

            uint result = 0;
            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask1) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask1Settings) & allSettings;

            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask2) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask2Settings) & allSettings;

            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask3) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask3Settings) & allSettings;

            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask4) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask4Settings) & allSettings;

            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.ListenBitmask5) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask5Settings) & allSettings;

            return result != 0;
        }

        public bool TalkSettingsEnabled(params BitmaskSettings[] settings)
        {
            uint allSettings = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                allSettings = allSettings | (uint)settings[i];
            }

            uint result = 0;
            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask1) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask1Settings) & allSettings;

            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask2) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask2Settings) & allSettings;

            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask3) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask3Settings) & allSettings;

            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask4) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask4Settings) & allSettings;

            if (((ChecksBitmask >> (int)ParticipantBitmaskMap.TalkBitmask5) & 1) != 0)
                result |= (ChecksBitmask >> (int)ParticipantBitmaskMap.Bitmask5Settings) & allSettings;

            return result != 0;
        }

        public bool TalkIntersectsListen(uint bitmask)
        {
            uint result = (ChecksBitmask & (0 << (int)ParticipantBitmaskMap.DataBitmask)) >> (int)ParticipantBitmaskMap.TalkBitmask1;
            result &= (bitmask & (0 << (int)ParticipantBitmaskMap.DataBitmask)) >> (int)ParticipantBitmaskMap.ListenBitmask1;

            return result != 0;
        }
    }

    public enum ParticipantBitmaskMap
    {
        Bitmask1Settings = 0, //4 bits
        Bitmask2Settings = 4, //4 bits
        Bitmask3Settings = 8, //4 bits
        Bitmask4Settings = 12, //4 bits
        Bitmask5Settings = 16, //4 bits
        TalkBitmask1 = 20, //1 bit
        TalkBitmask2 = 21, //1 bit
        TalkBitmask3 = 22, //1 bit
        TalkBitmask4 = 23, //1 bit
        TalkBitmask5 = 24, //1 bit
        ListenBitmask1 = 25, //1 bit
        ListenBitmask2 = 26, //1 bit
        ListenBitmask3 = 27, //1 bit
        ListenBitmask4 = 28, //1 bit
        ListenBitmask5 = 29, //1 bit
        DataBitmask = 30, //2 bits
        //32 bits total
    }

    public enum BitmaskSettings : uint
    {
        All = uint.MaxValue, //1111
        None = 0, //0000
        ProximityEnabled = 1, //0001
        DeathEnabled = 2, //0010
        VoiceEffectsEnabled = 4, //0100
        EnvironmentEnabled = 8, //1000
    }

    public enum DataBitmask : uint
    {
        Dead = 1,
        InWater = 2,
    }
}
