using System.Net;
using System.Net.Sockets;
using System.Numerics;
using VoiceCraft.Core.Packets;

namespace VoiceCraft.Core.Server
{
    public class VoiceCraftParticipant
    {
        //General Data
        public string Name { get; set; } = string.Empty;
        public PositioningTypes PositioningType { get; }
        public bool Binded { get; set; }
        public bool IsDeafened { get; set; }
        public bool IsMuted { get; set; }

        //Minecraft Data
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public float CaveDensity { get; set; }
        public bool IsDead { get; set; }
        public string EnvironmentId { get; set; } = string.Empty;
        public string MinecraftId { get; set; } = string.Empty;

        //Endpoints
        public Socket SignallingSocket { get; }
        public EndPoint? VoiceEndpoint { get; set; }

        public VoiceCraftParticipant(Socket SignallingSocket, PositioningTypes PositioningType)
        {
            this.PositioningType = PositioningType;
            this.SignallingSocket = SignallingSocket;
        }
    }
}
