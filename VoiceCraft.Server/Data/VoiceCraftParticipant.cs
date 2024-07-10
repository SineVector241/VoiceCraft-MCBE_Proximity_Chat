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
            get => ((ChecksBitmask >> (int)BitmaskLocation.DataBitmask) & (uint)DataBitmask.Dead) != 0;
            set
            {
                if (value)
                {
                    ChecksBitmask |= (uint)DataBitmask.Dead << (int)BitmaskLocation.DataBitmask;
                }
                else
                {
                    ChecksBitmask &= ~((uint)DataBitmask.Dead << (int)BitmaskLocation.DataBitmask);
                }
            }
        }
        public bool InWater
        {
            get => ((ChecksBitmask >> (int)BitmaskLocation.DataBitmask) & (uint)DataBitmask.InWater) != 0;
            set
            {
                if (value)
                {
                    ChecksBitmask |= (uint)DataBitmask.InWater << (int)BitmaskLocation.DataBitmask;
                }
                else
                {
                    ChecksBitmask &= ~((uint)DataBitmask.InWater << (int)BitmaskLocation.DataBitmask);
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

        public bool IntersectedSettingsEnabled(uint otherBitmask, params BitmaskSettings[] settings)
        {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint allListen = 
                (uint)BitmaskMap.ListenBitmask1 | 
                (uint)BitmaskMap.ListenBitmask2 | 
                (uint)BitmaskMap.ListenBitmask3 | 
                (uint)BitmaskMap.ListenBitmask4 | 
                (uint)BitmaskMap.ListenBitmask5;

            uint allTalk =
                (uint)BitmaskMap.TalkBitmask1 |
                (uint)BitmaskMap.TalkBitmask2 |
                (uint)BitmaskMap.TalkBitmask3 |
                (uint)BitmaskMap.TalkBitmask4 |
                (uint)BitmaskMap.TalkBitmask5;

            uint allSettings =
                (uint)BitmaskMap.Bitmask1Settings |
                (uint)BitmaskMap.Bitmask2Settings |
                (uint)BitmaskMap.Bitmask3Settings |
                (uint)BitmaskMap.Bitmask4Settings |
                (uint)BitmaskMap.Bitmask5Settings;

            uint intersectingBits = ((ChecksBitmask & allListen) >> (int)BitmaskLocation.ListenBitmask1) & ((otherBitmask & allTalk) >> (int)BitmaskLocation.TalkBitmask1); //Get all enabled intersecting bits.
            uint enabledTalkListenMasks = (intersectingBits << (int)BitmaskLocation.ListenBitmask1) | (intersectingBits << (int)BitmaskLocation.TalkBitmask1); //Duplicate the mask on both listen and talk bitmasks.
            uint mask = enabledTalkListenMasks | allSettings; //Create the mask.
            uint combinedSettings = GetEnabledListenSettings(ChecksBitmask & mask); //Isolate all settings and enabled bitmasks and get the enabled listen settings.
            combinedSettings |= GetEnabledTalkSettings(otherBitmask & mask); //Isolate all settings and enabled bitmasks and get the enabled talk settings.

            return (combinedSettings & settingsMask) != 0; //check any of the inputted settings match the combined settings.
        }

        public static uint GetEnabledTalkSettings(uint checksBitmask)
        {
            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask1) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask1Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask2) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask2Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask3) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask3Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask4) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask4Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask5) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask5Settings;

            return result;
        }

        public static uint GetEnabledListenSettings(uint checksBitmask)
        {
            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask1) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask1Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask2) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask2Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask3) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask3Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask4) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask4Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask5) != 0)
                result |= checksBitmask >> (int)BitmaskLocation.Bitmask5Settings;

            return result;
        }

        public static bool TalkSettingsEnabled(uint checksBitmask, params BitmaskSettings[] settings)
        {
            uint allSettings = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                allSettings |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask1) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask1Settings) & allSettings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask2) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask2Settings) & allSettings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask3) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask3Settings) & allSettings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask4) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask4Settings) & allSettings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask5) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask5Settings) & allSettings;

            return result != 0;
        }

        public static bool ListenSettingsEnabled(uint checksBitmask, params BitmaskSettings[] settings)
        {
            uint allSettings = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                allSettings |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask1) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask1Settings) & allSettings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask2) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask2Settings) & allSettings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask3) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask3Settings) & allSettings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask4) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask4Settings) & allSettings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask5) != 0)
                result |= (checksBitmask >> (int)BitmaskLocation.Bitmask5Settings) & allSettings;

            return result != 0;
        }
    }

    public enum BitmaskMap : uint
    {
        Bitmask1Settings = 0b00000000000000000000000000001111,
        Bitmask2Settings = 0b00000000000000000000000011110000,
        Bitmask3Settings = 0b00000000000000000000111100000000,
        Bitmask4Settings = 0b00000000000000001111000000000000,
        Bitmask5Settings = 0b00000000000011110000000000000000,
        TalkBitmask1 =     0b00000000000100000000000000000000,
        TalkBitmask2 =     0b00000000001000000000000000000000,
        TalkBitmask3 =     0b00000000010000000000000000000000,
        TalkBitmask4 =     0b00000000100000000000000000000000,
        TalkBitmask5 =     0b00000001000000000000000000000000,
        ListenBitmask1 =   0b00000010000000000000000000000000,
        ListenBitmask2 =   0b00000100000000000000000000000000,
        ListenBitmask3 =   0b00001000000000000000000000000000,
        ListenBitmask4 =   0b00010000000000000000000000000000,
        ListenBitmask5 =   0b00100000000000000000000000000000,
        DataBitmask =      0b11000000000000000000000000000000
    }

    public enum BitmaskLocation
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
