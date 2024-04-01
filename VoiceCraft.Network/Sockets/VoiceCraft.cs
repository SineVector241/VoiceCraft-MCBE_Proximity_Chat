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
        public Socket Socket { get; private set; } = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        public IPEndPoint RemoteEndpoint { get; private set; } = new IPEndPoint(IPAddress.Any, 0);
        public VoiceCraftSocketState State { get; private set; }
        public long LastActive { get; private set; } = 0; //Client Variable
        public int ActivityInterval { get; set; } = 1000;
        public int Timeout { get; set; } = 8000;
        public ushort Key { get; set; } = 0; //Client Variable
        public long Id { get; set; } //Client Variable

        //Private Variables
        private CancellationTokenSource CTS { get; set; } = new CancellationTokenSource();
        private ConcurrentDictionary<SocketAddress, EndPoint> EndPointBuffer { get; set; } = new ConcurrentDictionary<SocketAddress, EndPoint>();
        private ConcurrentDictionary<long, NetPeer> NetPeers { get; set; } = new ConcurrentDictionary<long, NetPeer>();
        private ConcurrentQueue<VoiceCraftPacket>? SendQueue { get; set; } //Client Variable
        private ConcurrentBag<VoiceCraftPacket>? ReliabilityQueue { get; set; } //Client Variable
        private Task? ActivityChecker { get; set; }
        private uint NextSequence { get; set; } //Client Variable
        private uint Sequence { get; set; } //Client Variable
        #endregion

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
        public async Task ConnectAsync(string IP, int port, ushort preferredKey, PositioningTypes positioningType, string version)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Started || State == VoiceCraftSocketState.Starting) throw new Exception("Cannot start connection as socket is in a hosting state!");
            if (State != VoiceCraftSocketState.Stopped) throw new Exception("You must disconnect before reconnecting!");

            //Reset/Setup
            State = VoiceCraftSocketState.Connecting;
            SendQueue = new ConcurrentQueue<VoiceCraftPacket>();
            ReliabilityQueue = new ConcurrentBag<VoiceCraftPacket>();
            NextSequence = 0;
            Sequence = 0;
            Key = preferredKey;
            Id = 0;

            //Register the Events
            OnAcceptReceived += OnAccept;
            OnDenyReceived += OnDeny;

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(IP, CTS.Token);
                if(addresses.Length == 0) throw new ArgumentException("Unable to retrieve address from the specified host name.", nameof(IP));
                RemoteEndpoint = new IPEndPoint(addresses[0], port);
                Send(new Login() { Key = preferredKey, PositioningType = positioningType, Version = version });

                while(!CTS.IsCancellationRequested) //Block until we are connected or timed out.
                {
                    await Task.Delay(1); //1 ms delay so we don't destroy the CPU.
                    if (Environment.TickCount64 - LastActive > Timeout)
                    {
                        throw new Exception("Connection Timed Out");
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

        public void Disconnect(string? reason = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State == VoiceCraftSocketState.Starting || State == VoiceCraftSocketState.Started) throw new InvalidOperationException("Cannot stop hosting as the socket is in a connection state.");
            if (State == VoiceCraftSocketState.Disconnecting) throw new InvalidOperationException("Already disconnecting.");
            if (State == VoiceCraftSocketState.Stopped) return;

            State = VoiceCraftSocketState.Disconnecting;
            //Deregister the Events
            OnAcceptReceived += OnAccept;
            OnDenyReceived += OnDeny;

            CTS.Cancel();
            SendQueue = null;
            ReliabilityQueue = null;
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
            if (State == VoiceCraftSocketState.Stopped) return;

            State = VoiceCraftSocketState.Stopping;
            await DisconnectPeers();
            CTS.Cancel();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            CTS = new CancellationTokenSource();
            OnStopped?.Invoke(reason);
        }

        public void Send(VoiceCraftPacket packet)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VoiceCraft));
            if (State != VoiceCraftSocketState.Connecting || State != VoiceCraftSocketState.Connected) throw new InvalidOperationException("Socket must be in a connecting or connected state to send packets!");

            if(packet.IsReliable && ReliabilityQueue != null)
            {
                packet.Sequence = Sequence++;
                packet.ResendTime = Environment.TickCount64 + 200;
                ReliabilityQueue.Add(packet);
            }

            if(SendQueue != null)
                SendQueue.Enqueue(packet);
        }

        public async Task DisconnectPeer(long Id, string? reason = null)
        {
            if (NetPeers.TryRemove(Id, out var peer))
            {
                await SocketSendToAsync(new Logout() { Id = peer.ID, Reason = reason ?? string.Empty, Sequence = peer.GetNextSequence() }, peer.EP); //Send immediately.
                peer.Dispose();
            }
        }

        private async Task DisconnectPeers()
        {
            foreach(var peerId in NetPeers.Keys)
            {
                await DisconnectPeer(peerId, null);
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

        private EndPoint GetEndPoint(SocketAddress receivedAddress)
        {
            if (!EndPointBuffer.TryGetValue(receivedAddress, out var endpoint))
            {
                // Create an EndPoint from the SocketAddress
                endpoint = RemoteEndpoint.Create(receivedAddress);

                var lookupCopy = new SocketAddress(receivedAddress.Family, receivedAddress.Size);
                receivedAddress.Buffer.CopyTo(lookupCopy.Buffer);

                EndPointBuffer[lookupCopy] = endpoint;
            }

            return endpoint;
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
                    var ep = GetEndPoint(receivedAddress);

                    //Do something with the received data.
                }
                catch (SocketException ex)
                {
                    await StopAsync(ex.Message);
                    return;
                }
            }
            await StopAsync();
        }

        private async Task ClientReceive()
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
                    //Disconnect Method Here
                    return;
                }
            }
            //Disconnect Method Here
        }

        private async Task ReceiveFromAsync(NetPeer peer)
        {
            byte[] buffer = GC.AllocateArray<byte>(length: 250, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();

            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var receivedBytes = await Socket.ReceiveFromAsync(bufferMem, SocketFlags.None, peer.EP, peer.CTS.Token);

                    //Do something with the received data.
                }
                catch (SocketException ex)
                {
                    //Remove the netpeer
                    return;
                }
            }

            //Stop method goes here.
        }

        private async Task ActivityCheck()
        {
            while (!CTS.IsCancellationRequested)
            {
                var dist = Environment.TickCount64 - LastActive;
                if (dist > Timeout)
                {
                    await Disconnect($"Signalling timed out!\nTime since last active {Environment.TickCount - LastActive}ms.");
                    break;
                }
                Send(new Ping() { Id = Id });
                await Task.Delay(ActivityInterval).ConfigureAwait(false);
            }
        }
        #endregion

        #region Client Event Methods
        private void OnAccept(Accept data, EndPoint endPoint)
        {
            Id = data.Id;
            Key = data.Key;
            OnConnected?.Invoke();
        }

        private void OnDeny(Deny data, EndPoint endPoint)
        {
            Disconnect(data.Reason);
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