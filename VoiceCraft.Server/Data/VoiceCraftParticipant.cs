using System.Net;
using System.Net.Sockets;
using System.Numerics;
using VoiceCraft.Core;

namespace VoiceCraft.Data.Server
{
    public class VoiceCraftParticipant : Participant
    {
        //General Data
        public PositioningTypes PositioningType { get; }
        public bool Binded { get; set; }
        public VoiceCraftChannel? Channel { get; set; } //Null channel is no channel
        public bool IsDeafened { get; set; }
        public bool IsMuted { get; set; }
        public bool IsServerMuted { get; set; }
        public int LastSpoke { get; set; }

        //Minecraft Data
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public float CaveDensity { get; set; }
        public bool IsDead { get; set; }
        public bool InWater { get; set; }
        public string EnvironmentId { get; set; } = string.Empty;
        public string MinecraftId { get; set; } = string.Empty;

        //Endpoints
        public Socket SignallingSocket { get; }
        public EndPoint? VoiceEndpoint { get; set; }

        public VoiceCraftParticipant(string name, ushort publicId, Socket signallingSocket, PositioningTypes positioningType) : base(name, publicId)
        {
            PositioningType = positioningType;
            SignallingSocket = signallingSocket;
        }
    }
}
