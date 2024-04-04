using System.Net.Sockets;
using System.Net;
using VoiceCraft.Core;
using VoiceCraft.Network.Packets;
using System.Collections.Concurrent;
using VoiceCraft.Network.Packets.VoiceCraft;
using System.Diagnostics;

namespace VoiceCraft.Network.Sockets
{
    public class VoiceCraft : Disposable
    {
        public const long MaxSendTime = 100;
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
        private Task? Sender { get; set; }
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

        public delegate void PacketData<T>(T data, NetPeer peer);

        //Error events
        public delegate void ExceptionError(Exception error);
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
        #endregion

        #region Methods
        public async Task ConnectAsync(string IP, int port, short preferredKey, PositioningTypes positioningType, string version)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Started || State == VoiceCraftSocketState.Starting) throw new Exception("Cannot start connection as socket is in a hosting state!");
            if (State != VoiceCraftSocketState.Stopped) throw new Exception("You must disconnect before reconnecting!");

            CTS.Dispose(); //Prevent memory leak for startup.

            //Reset/Setup
            State = VoiceCraftSocketState.Connecting;
            CTS = new CancellationTokenSource();
            ClientNetpeer = new NetPeer(RemoteEndpoint, long.MinValue, preferredKey);
            Sender = Task.Run(ClientSender);

            //Register the Events
            OnAcceptReceived += OnAccept;
            OnDenyReceived += OnDeny;
            OnLogoutReceived += OnLogout;
            OnAckReceived += OnAck;

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

                ActivityChecker = Task.Run(ActivityCheck);
                Send(new Login() { Key = preferredKey, PositioningType = positioningType, Version = version });
            }
            catch (Exception ex)
            {
                await DisconnectAsync(ex.Message, false);
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
            OnAckReceived -= OnAck;

            CTS.Cancel();
            CTS.Dispose();
            ClientNetpeer?.Dispose();
            ClientNetpeer = null;
            ActivityChecker = null;
            Sender = null;
            State = VoiceCraftSocketState.Stopped;
            OnDisconnected?.Invoke(reason);
        }

        public async Task HostAsync(int Port)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Connected || State == VoiceCraftSocketState.Connecting) throw new Exception("Cannot start hosting as socket is in a connection state!");
            if (State != VoiceCraftSocketState.Stopped) throw new Exception("You must stop hosting before starting a host!");

            CTS.Dispose(); //Prevent memory leak for startup.

            State = VoiceCraftSocketState.Starting;
            CTS = new CancellationTokenSource();

            OnLoginReceived += OnClientLogin;
            OnLogoutReceived += OnClientLogout;
            OnAckReceived += OnAck;

            try
            {
                RemoteEndpoint = new IPEndPoint(IPAddress.Any, Port);
                Socket.Bind(RemoteEndpoint);
                ActivityChecker = Task.Run(ServerCheck);
                Sender = Task.Run(ServerSender);
                State = VoiceCraftSocketState.Started;
                OnStarted?.Invoke();
                await ReceiveAsync();
            }
            catch (Exception ex)
            {
                await StopAsync(ex.Message);
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
            OnLoginReceived -= OnClientLogin;
            OnLogoutReceived -= OnClientLogout;
            OnAckReceived -= OnAck;

            await DisconnectPeers();
            CTS.Cancel();
            CTS.Dispose();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            ActivityChecker = null;
            Sender = null;
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

                if(peer.Connected)
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

                    if(packet.IsReliable)
                        netPeer.AddToSendBuffer(new Ack() { Id = netPeer.ID, PacketSequence = packet.Sequence });

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

            while (!CTS.IsCancellationRequested && !peer.Token.IsCancellationRequested)
            {
                try
                {
                    var receivedBytes = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, socketAddr, peer.Token);
                    var packet = PacketRegistry.GetPacketFromDataStream(bufferMem.ToArray());

                    if (packet.IsReliable)
                        peer.AddToSendBuffer(new Ack() { Id = peer.ID, PacketSequence = packet.Sequence });

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
                    var packet = PacketRegistry.GetPacketFromDataStream(bufferMem.ToArray());

                    if (packet.IsReliable)
                        ClientNetpeer?.AddToSendBuffer(new Ack() { Id = ClientNetpeer?.ID ?? long.MinValue, PacketSequence = packet.Sequence });

                    ClientNetpeer?.AddToReceiveBuffer(packet);
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
                    await DisconnectAsync($"Connection timed out!\nTime since last active {Environment.TickCount - ClientNetpeer?.LastActive}ms.", false);
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

        private async Task ServerSender()
        {
            while (!CTS.IsCancellationRequested)
            {
                foreach (var peer in NetPeers)
                {
                    var maxSendTime = Environment.TickCount64 + MaxSendTime;
                    VoiceCraftPacket? packet = null;
                    while (peer.Value.SendQueue.TryDequeue(out packet) && Environment.TickCount64 < maxSendTime && !CTS.IsCancellationRequested)
                    {
                        await SocketSendToAsync(packet, peer.Value.EP);
                    }
                }

                await Task.Delay(1); //1ms to not destroy the CPU.
            }
        }

        private async Task ClientSender()
        {
            while (!CTS.IsCancellationRequested && ClientNetpeer != null)
            {
                VoiceCraftPacket? packet = null;
                while (ClientNetpeer.SendQueue.TryDequeue(out packet) && !CTS.IsCancellationRequested)
                {
                    Debug.WriteLine($"Sending packet: {packet.ResendTime}, {packet.PacketId}");

                    await SocketSendAsync(packet);
                }

                await Task.Delay(1); //1ms to not destroy the CPU.
            }
        }

        private long GetAvailableId()
        {
            var Id = NetPeer.GenerateId();
            while(!IdExists(Id))
            {
                Id = NetPeer.GenerateId();
            }
            return Id;
        }

        private bool IdExists(long id)
        {
            foreach(var peer in NetPeers)
            {
                if(peer.Value.ID == id) return true;
            }
            return false;
        }

        private short GetAvailableKey()
        {
            var Key = NetPeer.GenerateKey();
            while (!KeyExists(Key))
            {
                Key = NetPeer.GenerateKey();
            }
            return Key;
        }

        private bool KeyExists(short key)
        {
            foreach (var peer in NetPeers)
            {
                if (peer.Value.Key == key) return true;
            }
            return false;
        }

        private void HandlePacketReceived(NetPeer peer, VoiceCraftPacket packet)
        {
            switch ((VoiceCraftPacketTypes)packet.PacketId)
            {
                case VoiceCraftPacketTypes.Login: OnLoginReceived?.Invoke((Login)packet, peer); break;
                case VoiceCraftPacketTypes.Logout: OnLogoutReceived?.Invoke((Logout)packet, peer); break;
                case VoiceCraftPacketTypes.Accept: OnAcceptReceived?.Invoke((Accept)packet, peer); break;
                case VoiceCraftPacketTypes.Deny: OnDenyReceived?.Invoke((Deny)packet, peer); break;
                case VoiceCraftPacketTypes.Ack: OnAckReceived?.Invoke((Ack)packet, peer); break;
                case VoiceCraftPacketTypes.Ping: OnPingReceived?.Invoke((Ping)packet, peer); break;
                case VoiceCraftPacketTypes.PingInfo: OnPingInfoReceived?.Invoke((PingInfo)packet, peer); break;
                case VoiceCraftPacketTypes.ParticipantJoined: OnParticipantJoinedReceived?.Invoke((ParticipantJoined)packet, peer); break;
                case VoiceCraftPacketTypes.ParticipantLeft: OnParticipantLeftReceived?.Invoke((ParticipantLeft)packet, peer); break;
                case VoiceCraftPacketTypes.Mute: OnMuteReceived?.Invoke((Mute)packet, peer); break;
                case VoiceCraftPacketTypes.Unmute: OnUnmuteReceived?.Invoke((Unmute)packet, peer); break;
                case VoiceCraftPacketTypes.Deafen: OnDeafenReceived?.Invoke((Deafen)packet, peer); break;
                case VoiceCraftPacketTypes.Undeafen: OnUndeafenReceived?.Invoke((Undeafen)packet, peer); break;
                case VoiceCraftPacketTypes.JoinChannel: OnJoinChannelReceived?.Invoke((JoinChannel)packet, peer); break;
                case VoiceCraftPacketTypes.LeaveChannel: OnLeaveChannelReceived?.Invoke((LeaveChannel)packet, peer); break;
                case VoiceCraftPacketTypes.AddChannel: OnAddChannelReceived?.Invoke((AddChannel)packet, peer); break;
                case VoiceCraftPacketTypes.RemoveChannel: OnRemoveChannelReceived?.Invoke((RemoveChannel)packet, peer); break;
                case VoiceCraftPacketTypes.UpdatePosition: OnUpdatePositionReceived?.Invoke((UpdatePosition)packet, peer); break;
                case VoiceCraftPacketTypes.ClientAudio: OnClientAudioReceived?.Invoke((ClientAudio)packet, peer); break;
                case VoiceCraftPacketTypes.ServerAudio: OnServerAudioReceived?.Invoke((ServerAudio)packet, peer); break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (State == VoiceCraftSocketState.Started || State == VoiceCraftSocketState.Starting)
                    StopAsync().Wait();

                if (State == VoiceCraftSocketState.Connected || State == VoiceCraftSocketState.Connecting)
                    DisconnectAsync().Wait();

                Socket.Dispose();
            }
        }
        #endregion

        #region Client Event Methods
        private void OnAccept(Accept data, NetPeer peer)
        {
            if(ClientNetpeer != null)
            {
                ClientNetpeer.ID = data.Id;
                ClientNetpeer.Key = data.Key;
            }
            OnConnected?.Invoke();
        }

        private void OnDeny(Deny data, NetPeer peer)
        {
            DisconnectAsync(data.Reason, false).Wait();
        }

        private void OnLogout(Logout data, NetPeer peer)
        {
            if(data.Id == peer.ID)
                DisconnectAsync(data.Reason, false).Wait();
        }
        #endregion

        #region Server Event Methods
        private void OnClientLogin(Login data, NetPeer peer)
        {
            var Id = GetAvailableId();
            var key = data.Key;
            if (KeyExists(key))
                key = GetAvailableKey();

            peer.ID = Id;
            peer.Key = key;
            OnPeerConnected?.Invoke(peer); //Leave wether the client should be accepted or denied by the application.
        }

        private void OnClientLogout(Logout data, NetPeer peer)
        {
            if (data.Id == peer.ID)
            {
                var key = NetPeers.FirstOrDefault(x => x.Value == peer).Key;
                DisconnectPeer(key, false).Wait();
            }
        }
        #endregion

        #region Global Event Methods
        private void OnAck(Ack data, NetPeer peer)
        {
            peer.AcknowledgePacket(data.PacketSequence);
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