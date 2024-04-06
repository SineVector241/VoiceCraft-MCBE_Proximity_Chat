using System.Collections.Concurrent;
using System.Net;
using VoiceCraft.Core;
using VoiceCraft.Core.Packets.VoiceCraft;
using VoiceCraft.Network;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server
{
    public class VoiceCraftServer
    {
        public const string Version = "v1.0.4";
        public ConcurrentDictionary<NetPeer, VoiceCraftParticipant> Participants { get; set; } = new ConcurrentDictionary<NetPeer, VoiceCraftParticipant>();
        public Network.Sockets.VoiceCraft VoiceCraftSocket { get; set; }
        public Network.Sockets.MCComm MCComm { get; set; }
        public Properties ServerProperties { get; set; }
        public List<string> Banlist { get; set; }

        #region Delegates
        public delegate void ParticipantJoined(VoiceCraftParticipant participant);
        public delegate void ParticipantLeft(VoiceCraftParticipant participant, string? reason = null);
        public delegate void Failed(Exception ex);
        #endregion

        #region Events
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event Failed? OnFailed;
        #endregion

        public VoiceCraftServer(Properties properties, List<string> banlist)
        {
            ServerProperties = properties;
            Banlist = banlist;
            VoiceCraftSocket = new Network.Sockets.VoiceCraft();
            MCComm = new Network.Sockets.MCComm();

            VoiceCraftSocket.OnPeerConnected += OnPeerConnected;
            VoiceCraftSocket.OnPeerDisconnected += OnPeerDisconnected;
        }

        public void Start()
        {
            _ = Task.Run(async () => {
                try
                {
                    await VoiceCraftSocket.HostAsync(ServerProperties.VoiceCraftPortUDP);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    OnFailed?.Invoke(ex);
                }
            });
        }

        #region VoiceCraft Event Methods
        private void OnPeerConnected(NetPeer peer, Login packet)
        {
            if (Version != packet.Version)
            {
                peer.DenyLogin("Versions do not match!");
                return;
            }
            if (Banlist.Exists(x => x == ((IPEndPoint)peer.RemoteEndPoint).Address.ToString()))
            {
                peer.DenyLogin("You have been banned from the server!");
                return;
            }
            if (packet.PositioningType != PositioningTypes.ClientSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Client || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                peer.DenyLogin("Server only accepts client sided positioning!");
                return;
            }
            else if (packet.PositioningType != PositioningTypes.ServerSided &&
                (ServerProperties.ConnectionType == ConnectionTypes.Server || ServerProperties.ConnectionType == ConnectionTypes.Hybrid))
            {
                peer.DenyLogin("Server only accepts server sided positioning!");
                return;
            }
            peer.AcceptLogin();
            var participant = new VoiceCraftParticipant(string.Empty, peer);
            Participants.TryAdd(peer, participant);
            OnParticipantJoined?.Invoke(participant);
        }

        private void OnPeerDisconnected(NetPeer peer, string? reason = null)
        {
            if(Participants.TryRemove(peer, out var participant))
            {
                OnParticipantLeft?.Invoke(participant, reason);
            }
        }
        #endregion
    }
}
