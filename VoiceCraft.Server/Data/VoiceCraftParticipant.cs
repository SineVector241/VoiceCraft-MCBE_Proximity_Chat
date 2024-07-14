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
        public float EchoFactor { get; set; }
        public bool Muffled { get; set; }
        public bool Dead
        {
            get => ((ChecksBitmask >> (int)BitmaskLocations.DataBitmask) & (uint)DataBitmask.Dead) != 0;
            set
            {
                if (value)
                {
                    ChecksBitmask |= (uint)DataBitmask.Dead << (int)BitmaskLocations.DataBitmask;
                }
                else
                {
                    ChecksBitmask &= ~((uint)DataBitmask.Dead << (int)BitmaskLocations.DataBitmask);
                }
            }
        }
        public string EnvironmentId { get; set; } = string.Empty;
        public string MinecraftId { get; set; } = string.Empty;
        public uint ChecksBitmask { get; set; } = (uint)BitmaskMap.Default;

        public VoiceCraftParticipant(string name, Channel channel) : base(name)
        {
            Channel = channel;
        }

        public static short GenerateKey()
        {
            return (short)Random.Shared.Next(short.MinValue + 1, short.MaxValue); //short.MinValue is used to specify no Key.
        }

        public uint GetIntersectedTalkBitmasks(uint otherBitmask)
        {
            return ((ChecksBitmask & (uint)BitmaskMap.AllTalkBitmasks) >> (int)BitmaskLocations.TalkBitmask1) & ((otherBitmask & (uint)BitmaskMap.AllListenBitmasks) >> (int)BitmaskLocations.ListenBitmask1); //Get all disabled intersecting bits.
        }

        public uint GetIntersectedListenBitmasks(uint otherBitmask)
        {
            return ((ChecksBitmask & (uint)BitmaskMap.AllListenBitmasks) >> (int)BitmaskLocations.ListenBitmask1) & ((otherBitmask & (uint)BitmaskMap.AllTalkBitmasks) >> (int)BitmaskLocations.TalkBitmask1); //Get all disabled intersecting bits.
        }

        public bool IntersectedTalkSettingsDisabled(uint otherBitmask, params BitmaskSettings[] settings)
        {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint intersectingBits = GetIntersectedTalkBitmasks(otherBitmask);
            uint disabledTalkMasks = intersectingBits << (int)BitmaskLocations.TalkBitmask1; //Move into the talk bitmask area.
            uint mask = disabledTalkMasks | (uint)BitmaskMap.AllBitmaskSettings; //Create the mask.
            uint talkSettings = GetDisabledTalkSettings(ChecksBitmask & mask); //Isolate all settings and disabled bitmasks and get the disabled talk settings.

            return (talkSettings & settingsMask) != 0; //check if any of the inputted settings match the combined settings.
        }

        public bool IntersectedListenSettingsDisabled(uint otherBitmask, params BitmaskSettings[] settings)
        {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint intersectingBits = GetIntersectedListenBitmasks(otherBitmask);
            uint disabledListenMasks = intersectingBits << (int)BitmaskLocations.ListenBitmask1; //Move into the listen bitmask area.
            uint mask = disabledListenMasks | (uint)BitmaskMap.AllBitmaskSettings; //Create the mask.
            uint listenSettings = GetDisabledListenSettings(ChecksBitmask & mask); //Isolate all settings and disabled bitmasks and get the disabled listen settings.

            return (listenSettings & settingsMask) != 0; //check if any of the inputted settings match the combined settings.
        }

        public static uint GetDisabledTalkSettings(uint checksBitmask)
        {
            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask1) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask1Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask2) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask2Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask3) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask3Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask4) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask4Settings;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask5) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask5Settings;

            return result;
        }

        public static uint GetDisabledListenSettings(uint checksBitmask)
        {
            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask1) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask1Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask2) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask2Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask3) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask3Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask4) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask4Settings;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask5) != 0)
                result |= checksBitmask >> (int)BitmaskLocations.Bitmask5Settings;

            return result;
        }

        public static bool TalkSettingsDisabled(uint checksBitmask, params BitmaskSettings[] settings)
        {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask1) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask1Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask2) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask2Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask3) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask3Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask4) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask4Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.TalkBitmask5) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask5Settings) & settingsMask;

            return result != 0;
        }

        public static bool ListenSettingsDisabled(uint checksBitmask, params BitmaskSettings[] settings)
        {
            uint settingsMask = 0;
            for (int i = 0; i < settings.Length; i++)
            {
                settingsMask |= (uint)settings[i]; //Combine all settings to compare against.
            }

            uint result = 0;
            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask1) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask1Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask2) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask2Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask3) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask3Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask4) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask4Settings) & settingsMask;

            if ((checksBitmask & (uint)BitmaskMap.ListenBitmask5) != 0)
                result |= (checksBitmask >> (int)BitmaskLocations.Bitmask5Settings) & settingsMask;

            return result != 0;
        }
    }

    public enum BitmaskMap : uint
    {
        Default = TalkBitmask1 | ListenBitmask1,
        AllBitmaskSettings = Bitmask1Settings | Bitmask2Settings | Bitmask3Settings | Bitmask4Settings | Bitmask5Settings,
        AllTalkBitmasks = TalkBitmask1 | TalkBitmask2 | TalkBitmask3 | TalkBitmask4 | TalkBitmask5,
        AllListenBitmasks = ListenBitmask1 | ListenBitmask2 | ListenBitmask3 | ListenBitmask4 | ListenBitmask5,

        Bitmask1Settings = 0b00000000000000000000000000001111,
        Bitmask2Settings = 0b00000000000000000000000011110000,
        Bitmask3Settings = 0b00000000000000000000111100000000,
        Bitmask4Settings = 0b00000000000000001111000000000000,
        Bitmask5Settings = 0b00000000000011110000000000000000,
        TalkBitmask1 = 0b00000000000100000000000000000000,
        TalkBitmask2 = 0b00000000001000000000000000000000,
        TalkBitmask3 = 0b00000000010000000000000000000000,
        TalkBitmask4 = 0b00000000100000000000000000000000,
        TalkBitmask5 = 0b00000001000000000000000000000000,
        ListenBitmask1 = 0b00000010000000000000000000000000,
        ListenBitmask2 = 0b00000100000000000000000000000000,
        ListenBitmask3 = 0b00001000000000000000000000000000,
        ListenBitmask4 = 0b00010000000000000000000000000000,
        ListenBitmask5 = 0b00100000000000000000000000000000,
        DataBitmask = 0b11000000000000000000000000000000
    }

    public enum BitmaskLocations
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
        ProximityDisabled = 1, //0001
        DeathDisabled = 2, //0010
        VoiceEffectsDisabled = 4, //0100
        EnvironmentDisabled = 8, //1000
    }

    public enum DataBitmask : uint
    {
        Dead = 1,
    }
}
