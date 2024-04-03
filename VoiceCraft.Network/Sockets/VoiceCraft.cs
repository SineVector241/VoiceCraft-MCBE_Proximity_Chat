using System.Net.Sockets;
using System.Net;
using VoiceCraft.Core;
using VoiceCraft.Network.Packets;
using System.Collections.Concurrent;
using VoiceCraft.Network.Packets.VoiceCraft;

namespace VoiceCraft.Network.Sockets
{
    public class VoiceCraft : Disposable
    {
        #region Variables
        //Public Variables
        public PacketRegistry PacketRegistry { get; set; } = new PacketRegistry();
        public Socket Socket { get; private set; } = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        public IPEndPoint RemoteEndpoint { get; private set; } = new IPEndPoint(IPAddress.Any, 0);
        public VoiceCraftSocketState State { get; private set; }
        public int Timeout { get; set; } = 8000;

        //Private Variables
        private CancellationTokenSource CTS { get; set; } = new CancellationTokenSource();
        private ConcurrentDictionary<SocketAddress, NetPeer> NetPeers { get; set; } = new ConcurrentDictionary<SocketAddress, NetPeer>(); //Server Variable
        private NetPeer? ClientNetpeer { get; set; } //Client Variable
        private Task? ActivityChecker { get; set; }
        #endregion

        public VoiceCraft()
        {
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Login, typeof(Login));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Logout, typeof(Logout));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Accept, typeof(Accept));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Deny, typeof(Deny));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Ack, typeof(Ack));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Ping, typeof(Ping));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.PingInfo, typeof(PingInfo));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ParticipantJoined, typeof(ParticipantJoined));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ParticipantLeft, typeof(ParticipantLeft));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Mute, typeof(Mute));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Unmute, typeof(Unmute));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Deafen, typeof(Deafen));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.Undeafen, typeof(Undeafen));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.JoinChannel, typeof(JoinChannel));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.LeaveChannel, typeof(LeaveChannel));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.AddChannel, typeof(AddChannel));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.RemoveChannel, typeof(RemoveChannel));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.UpdatePosition, typeof(UpdatePosition));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ClientAudio, typeof(ClientAudio));
            PacketRegistry.RegisterPacket((byte)VoiceCraftPacketTypes.ServerAudio, typeof(ServerAudio));
        }

        #region Debug Settings
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<VoiceCraftPacketTypes> InboundFilter { get; set; } = new List<VoiceCraftPacketTypes>();
        public List<VoiceCraftPacketTypes> OutboundFilter { get; set; } = new List<VoiceCraftPacketTypes>();
        #endregion

        #region Delegates
        public delegate void Connected();
        public delegate void Disconnected(string? reason = null);

        public delegate void Started();
        public delegate void Stopped(string? reason = null);
        public delegate void PeerConnected(NetPeer peer);
        public delegate void PeerDisconnected(NetPeer peer, string? reason = null);

        public delegate void PacketData<T>(T data, EndPoint endPoint);

        //Error events
        public delegate void ExceptionError(Exception error);

        //Regular Events
        public delegate void Failed(Exception error);
        #endregion

        #region Events
        //Client Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;

        //Server Events
        public event Started? OnStarted;
        public event Stopped? OnStopped;
        public event PeerConnected? OnPeerConnected;
        public event PeerDisconnected? OnPeerDisconnected;

        //Packet Events
        public event PacketData<Login>? OnLoginReceived;
        public event PacketData<Logout>? OnLogoutReceived;
        public event PacketData<Accept>? OnAcceptReceived;
        public event PacketData<Deny>? OnDenyReceived;
        public event PacketData<Ack>? OnAckReceived;
        public event PacketData<Ping>? OnPingReceived;
        public event PacketData<PingInfo>? OnPingInfoReceived;
        public event PacketData<ParticipantJoined>? OnParticipantJoinedReceived;
        public event PacketData<ParticipantLeft>? OnParticipantLeftReceived;
        public event PacketData<Mute>? OnMuteReceived;
        public event PacketData<Unmute>? OnUnmuteReceived;
        public event PacketData<Deafen>? OnDeafenReceived;
        public event PacketData<Undeafen>? OnUndeafenReceived;
        public event PacketData<JoinChannel>? OnJoinChannelReceived;
        public event PacketData<LeaveChannel>? OnLeaveChannelReceived;
        public event PacketData<AddChannel>? OnAddChannelReceived;
        public event PacketData<RemoveChannel>? OnRemoveChannelReceived;
        public event PacketData<UpdatePosition>? OnUpdatePositionReceived;
        public event PacketData<ClientAudio>? OnClientAudioReceived;
        public event PacketData<ServerAudio>? OnServerAudioReceived;

        //Error Events
        public event ExceptionError? OnExceptionError;

        //Regular Events
        public event Failed? OnFailed;
        #endregion

        #region Methods
        public async Task ConnectAsync(string IP, int port, short preferredKey, PositioningTypes positioningType, string version)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Started || State == VoiceCraftSocketState.Starting) throw new Exception("Cannot start connection as socket is in a hosting state!");
            if (State != VoiceCraftSocketState.Stopped) throw new Exception("You must disconnect before reconnecting!");

            //Reset/Setup
            State = VoiceCraftSocketState.Connecting;
            ClientNetpeer = new NetPeer(RemoteEndpoint, long.MinValue, preferredKey);

            //Register the Events
            OnAcceptReceived += OnAccept;
            OnDenyReceived += OnDeny;
            OnLogoutReceived += OnLogout;

            try
            {
                if (IPAddress.TryParse(IP, out var ip))
                {
                    RemoteEndpoint = new IPEndPoint(ip, port);
                }
                else
                {
                    var addresses = await Dns.GetHostAddressesAsync(IP, CTS.Token);
                    if (addresses.Length == 0) throw new ArgumentException("Unable to retrieve address from the specified host name.", nameof(IP));
                    RemoteEndpoint = new IPEndPoint(addresses[0], port);
                }

                State = VoiceCraftSocketState.Connecting;
                Send(new Login() { Key = preferredKey, PositioningType = positioningType, Version = version });

                while (!CTS.IsCancellationRequested) //Block until we are connected or timed out.
                {
                    await Task.Delay(1); //1 ms delay so we don't destroy the CPU.
                    if (Environment.TickCount64 - ClientNetpeer.LastActive > Timeout)
                    {
                        throw new Exception("Connection Timed Out.");
                    }
                    else if (State == VoiceCraftSocketState.Connected)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                State = VoiceCraftSocketState.Stopped;
                OnFailed?.Invoke(ex);
            }
        }

        public async Task DisconnectAsync(string? reason = null, bool notifyServer = true)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Starting || State == VoiceCraftSocketState.Started) throw new InvalidOperationException("Cannot stop hosting as the socket is in a connection state.");
            if (State == VoiceCraftSocketState.Disconnecting) throw new InvalidOperationException("Already disconnecting.");
            if (State == VoiceCraftSocketState.Stopped) return;

            //We don't need to wait until we are connected because the Cancellation Token already takes care of cancelling other thread related requests.

            if (notifyServer && State == VoiceCraftSocketState.Connected) //Only send if we are connected.
                await SocketSendAsync(new Logout() { Id = ClientNetpeer?.ID ?? long.MinValue });

            State = VoiceCraftSocketState.Disconnecting;
            //Deregister the Events
            OnAcceptReceived -= OnAccept;
            OnDenyReceived -= OnDeny;
            OnLogoutReceived -= OnLogout;

            CTS.Cancel();
            CTS.Dispose();
            CTS = new CancellationTokenSource();
            ClientNetpeer?.Dispose();
            ClientNetpeer = null;
            ActivityChecker = null;
            State = VoiceCraftSocketState.Stopped;
            OnDisconnected?.Invoke(reason);
        }

        public async Task HostAsync(int Port)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Connected || State == VoiceCraftSocketState.Connecting) throw new Exception("Cannot start hosting as socket is in a connection state!");
            if (State != VoiceCraftSocketState.Stopped) throw new Exception("You must stop hosting before starting a host!");

            State = VoiceCraftSocketState.Starting;
            try
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, Port);
                Socket.Bind(RemoteEndpoint);
                ActivityChecker = Task.Run(ServerCheck);
                State = VoiceCraftSocketState.Started;
                OnStarted?.Invoke();
                await ReceiveAsync();
            }
            catch (Exception ex)
            {
                State = VoiceCraftSocketState.Stopped;
                OnFailed?.Invoke(ex);
            }
        }

        public async Task StopAsync(string? reason = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Connecting || State == VoiceCraftSocketState.Connected) throw new InvalidOperationException("Cannot stop hosting as the socket is in a connection state.");
            if (State == VoiceCraftSocketState.Stopping) throw new InvalidOperationException("Already stopping.");

            while (State == VoiceCraftSocketState.Starting) //Wait until started then we stop.
            {
                await Task.Delay(1); //1ms delay so we don't destroy the CPU.
            }

            if (State == VoiceCraftSocketState.Stopped) return;

            State = VoiceCraftSocketState.Stopping;
            await DisconnectPeers();
            CTS.Cancel();
            CTS.Dispose();
            Socket.Close();
            CTS = new CancellationTokenSource();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ActivityChecker = null;
            State = VoiceCraftSocketState.Stopped;
            OnStopped?.Invoke(reason);
        }

        public void Send(VoiceCraftPacket packet)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Connecting || State == VoiceCraftSocketState.Connected)
            {
                if (ClientNetpeer != null)
                {
                    ClientNetpeer.AddToSendBuffer(packet);
                }
            }
            else
                throw new InvalidOperationException("Socket must be in a connecting or connected state to send packets!");
        }

        public async Task DisconnectPeer(SocketAddress Address, bool notifyPeer = false, string? reason = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));

            if (NetPeers.TryRemove(Address, out var peer))
            {
                if(notifyPeer && State == VoiceCraftSocketState.Started)
                    await SocketSendToAsync(new Logout() { Id = peer.ID, Reason = reason ?? string.Empty }, peer.EP); //Send immediately.
                peer.OnPacketReceived -= HandlePacketReceived;
                peer.Dispose();
                OnPeerDisconnected?.Invoke(peer, reason);
            }
        }

        private async Task DisconnectPeers()
        {
            foreach(var peerSocket in NetPeers.Keys)
            {
                await DisconnectPeer(peerSocket, true, null);
            }
        }

        private async Task SocketSendToAsync(VoiceCraftPacket packet, EndPoint ep)
        {
            var buffer = new List<byte>();
            packet.WritePacket(ref buffer);
            await Socket.SendToAsync(buffer.ToArray(), ep, CTS.Token);
        }

        private async Task SocketSendAsync(VoiceCraftPacket packet)
        {
            var buffer = new List<byte>();
            packet.WritePacket(ref buffer);
            await Socket.SendToAsync(buffer.ToArray(), RemoteEndpoint, CTS.Token);
        }

        private NetPeer GetOrCreateNetPeer(SocketAddress receivedAddress)
        {
            if (!NetPeers.TryGetValue(receivedAddress, out var netPeer))
            {
                // Create an EndPoint from the SocketAddress
                netPeer = new NetPeer(RemoteEndpoint.Create(receivedAddress), long.MinValue, short.MinValue);
                netPeer.OnPacketReceived += HandlePacketReceived;

                var lookupCopy = new SocketAddress(receivedAddress.Family, receivedAddress.Size);
                receivedAddress.Buffer.CopyTo(lookupCopy.Buffer);

                NetPeers[lookupCopy] = netPeer;
            }

            return netPeer;
        }

        private async Task ReceiveAsync()
        {
            byte[] buffer = GC.AllocateArray<byte>(length: 250, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();
            var receivedAddress = new SocketAddress(Socket.AddressFamily);

            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var receivedBytes = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, receivedAddress, CTS.Token);
                    var netPeer = GetOrCreateNetPeer(receivedAddress);
                    var packet = PacketRegistry.GetPacketFromDataStream(bufferMem.ToArray());
                    netPeer.AddToReceiveBuffer(packet);
                }
                catch (SocketException ex)
                {
                    await StopAsync(ex.Message);
                    return;
                }
            }
        }

        private async Task ReceiveFromAsync(SocketAddress socketAddr, NetPeer peer)
        {
            byte[] buffer = GC.AllocateArray<byte>(length: 250, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();

            while (!CTS.IsCancellationRequested && !peer.CTS.IsCancellationRequested)
            {
                try
                {
                    var receivedBytes = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, socketAddr, peer.CTS.Token);
                    var packet = PacketRegistry.GetPacketFromDataStream(bufferMem.ToArray());
                    peer.AddToReceiveBuffer(packet);
                }
                catch (SocketException ex)
                {
                    await DisconnectPeer(socketAddr, false, ex.Message); //Socket is basically closed at this point, We can't send a message.
                    return;
                }
            }
        }

        private async Task ClientReceiveAsync()
        {
            byte[] buffer = GC.AllocateArray<byte>(length: 250, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();

            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var receivedBytes = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, RemoteEndpoint, CTS.Token);

                    //Do something with the received data.
                }
                catch (SocketException ex)
                {
                    await DisconnectAsync(ex.Message, false); //Socket is basically closed at this point, We can't send a message.
                    return;
                }
            }
        }

        private async Task ActivityCheck()
        {
            var time = Environment.TickCount64;
            while (!CTS.IsCancellationRequested)
            {
                var dist = Environment.TickCount64 - ClientNetpeer?.LastActive;
                if (dist > Timeout)
                {
                    await DisconnectAsync($"Signalling timed out!\nTime since last active {Environment.TickCount - ClientNetpeer?.LastActive}ms.", false);
                    break;
                }
                ClientNetpeer?.ResendPackets();

                if (Environment.TickCount64 - time >= 1000) //1 second ping interval.
                {
                    Send(new Ping() { Id = ClientNetpeer?.ID ?? long.MinValue });
                    time = Environment.TickCount64;
                }
                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        private async Task ServerCheck()
        {
            while (!CTS.IsCancellationRequested)
            {
                for (int i = NetPeers.Count - 1; i >= 0; i--)
                {
                    var peer = NetPeers.ElementAt(i);

                    if (Environment.TickCount64 - peer.Value.LastActive > Timeout)
                    {
                        await DisconnectPeer(peer.Key, true, $"Timeout - Last Active: {Environment.TickCount64 - peer.Value.LastActive}ms");
                    }
                }

                foreach (var peer in NetPeers)
                {
                    peer.Value.ResendPackets();
                }

                await Task.Delay(1).ConfigureAwait(false);
            }
        }

        private async Task HandlePacketReceived(NetPeer peer, VoiceCraftPacket packet)
        {
            switch ((VoiceCraftPacketTypes)packet.PacketId)
            {
                case VoiceCraftPacketTypes.Login:
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (State == VoiceCraftSocketState.Started || State == VoiceCraftSocketState.Starting)
                StopAsync().Wait();
        }
        #endregion

        #region Client Event Methods
        private void OnAccept(Accept data, EndPoint endPoint)
        {
            if(ClientNetpeer != null)
            {
                ClientNetpeer.ID = data.Id;
                ClientNetpeer.Key = data.Key;
            }    
            ActivityChecker = Task.Run(ActivityCheck);
            OnConnected?.Invoke();
        }

        private void OnDeny(Deny data, EndPoint endPoint)
        {
            DisconnectAsync(data.Reason, false).Wait();
        }

        private void OnLogout(Logout data, EndPoint endPoint)
        {
            DisconnectAsync(data.Reason, false).Wait();
        }
        #endregion
    }

    public enum VoiceCraftSocketState
    {
        Stopped,

        //Client
        Connecting,
        Connected,
        Disconnecting,

        //Hoster
        Starting,
        Started,
        Stopping
    }
}