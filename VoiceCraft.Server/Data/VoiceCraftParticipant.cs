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
        public bool Dead { get; set; }
        public bool InWater { get; set; }
        public string EnvironmentId { get; set; } = string.Empty;
        public string MinecraftId { get; set; } = string.Empty;
        public ushort ChecksBitmask { get; set; } = ushort.MaxValue; //All bits are set. 1111 1111 1111 1111

        public VoiceCraftParticipant(string name, Channel channel) : base(name)
        {
            Channel = channel;
        }

        public static short GenerateKey()
        {
            return (short)Random.Shared.Next(short.MinValue + 1, short.MaxValue); //short.MinValue is used to specify no Key.
        }
    }

    public enum ParticipantBitmask : ushort
    {
        All = ushort.MaxValue, // 1111 1111 1111 1111
        None = 0, // 0000 0000 0000 0000
        DeathEnabled = 1, // 0000 0000 0000 0001
        ProximityEnabled = 2, // 0000 0000 0000 0010
        WaterEffectEnabled = 4, // 0000 0000 0000 0100
        EchoEffectEnabled = 8, // 0000 0000 0000 1000
        DirectionalEnabled = 16, // 0000 0000 0001 0000
        EnvironmentEnabled = 32, // 0000 0000 0010 0000

        HearingBitmask1 = 64, // 0000 0000 0100 0000
        HearingBitmask2 = 128, // 0000 0000 1000 0000
        HearingBitmask3 = 256, // 0000 0001 0000 0000
        HearingBitmask4 = 512, // 0000 0010 0000 0000
        HearingBitmask5 = 1024, // 0000 0100 0000 0000

        TalkingBitmask1 = 2048, // 0000 1000 0000 0000
        TalkingBitmask2 = 4096, // 0001 0000 0000 0000
        TalkingBitmask3 = 8192, // 0010 0000 0000 0000
        TalkingBitmask4 = 16384, // 0100 0000 0000 0000
        TalkingBitmask5 = 32768, // 1000 0000 0000 0000
    }
}
