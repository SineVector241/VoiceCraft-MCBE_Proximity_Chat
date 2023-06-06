using System.Collections.Concurrent;
using System.Numerics;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Network.Packets;

namespace VoiceCraft.Mobile.Interfaces
{
    public interface INetworkManager
    {
        //Variables
        public string IP { get; }
        public int Port { get; }
        public uint Key { get; }
        public bool ClientPositioning { get; }
        public Codecs Codec { get; }

        public ConcurrentDictionary<uint, ParticipantModel> Participants { get; }
        public INetwork Signalling { get; }
        public INetwork Voice { get; }
#nullable enable
        public INetwork? Websocket { get; }
#nullable disable

        //Events
        public delegate void Connected(NetworkSockets networkSocket, uint Key);
        public delegate void ConnectionError(string reason);
#nullable enable
        public delegate void Disconnected(string? reason);
#nullable disable
        public delegate void ParticipantConnect(ParticipantModel Participant);
        public delegate void ParticipantDisconnect(ParticipantModel Participant);
        public delegate void AudioReceived();

        public event Connected OnConnect;
        public event Disconnected OnDisconnect;
        public event ConnectionError OnConnectError;
        public event ParticipantConnect OnParticipantConnect;
        public event ParticipantDisconnect OnParticipantDisconnect;
        public event AudioReceived OnAudioReceived;

        //Methods
        public void Connect();

#nullable enable
        public void Disconnect(string? reason);
        public void SendAudio(byte[] audio, Vector3? position, Vector3? velocity);
        public void SendRawPacket(VoicePacket packet);
        public void SendRawPacket(SignallingPacket packet);
#nullable disable
    }

    public enum NetworkSockets
    {
        Signalling,
        Voice,
        Websocket
    }
}
