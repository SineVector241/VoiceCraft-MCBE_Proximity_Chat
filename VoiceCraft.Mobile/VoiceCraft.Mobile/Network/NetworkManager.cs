using System.Collections.Concurrent;
using System.Numerics;
using VoiceCraft.Mobile.Interfaces;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Network.Packets;
using VoiceCraft.Mobile.Network.Sockets;

namespace VoiceCraft.Mobile.Network
{
    public class NetworkManager : INetworkManager
    {
        public string IP { get; }

        public int Port { get; }

        public uint Key { get; }

        public bool ClientPositioning { get; }

        public Codecs Codec { get; }

        public ConcurrentDictionary<uint, ParticipantModel> Participants { get; }

        public INetwork Signalling { get; }

        public INetwork Voice { get; }

        public INetwork Websocket { get; }

        public event INetworkManager.Connected OnConnect;
        public event INetworkManager.ConnectionError OnConnectError;
        public event INetworkManager.Disconnected OnDisconnect;
        public event INetworkManager.ParticipantConnect OnParticipantConnect;
        public event INetworkManager.ParticipantDisconnect OnParticipantDisconnect;
        public event INetworkManager.AudioReceived OnAudioReceived;

        public NetworkManager(string IP, int Port, uint Key, bool ClientSidedPositioning, Codecs Codec)
        {
            this.IP = IP;
            this.Port = Port;
            this.Key = Key;
            this.Codec = Codec;
            ClientPositioning = ClientSidedPositioning;

            Participants = new ConcurrentDictionary<uint, ParticipantModel>();
        }

        public void Connect()
        {
            
        }

        public void Disconnect(string reason)
        {
        }

        public void SendAudio(byte[] audio, Vector3? position, Vector3? velocity)
        {
        }

        public void SendRawPacket(VoicePacket packet)
        {
        }

        public void SendRawPacket(SignallingPacket packet)
        {
        }
    }
}
