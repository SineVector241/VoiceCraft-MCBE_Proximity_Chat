using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.Voice;

namespace VoiceCraft.Core.Sockets
{
    public class Voice
    {
        #region Variables
        private CancellationTokenSource CTS { get; set; }
        public Socket Socket { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsHosting { get; private set; }
        public bool IsDisposed { get; private set; }
        public int ActivityInterval { get; set; } = 5000;
        private IPEndPoint IPListener { get; set; } = new IPEndPoint(IPAddress.Any, 0);
        private Task? ActivityChecker { get; set; }

        //Debug Settings
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<VoicePacketTypes> InboundFilter { get; set; } = new List<VoicePacketTypes>();
        public List<VoicePacketTypes> OutboundFilter { get; set; } = new List<VoicePacketTypes>();
        #endregion

        #region Delegates
        public delegate void Connected();
        public delegate void Disconnected(string? reason = null);
        public delegate void PacketData<T>(T data, EndPoint endPoint);

        public delegate void OutboundPacket(VoicePacket packet, EndPoint endPoint);
        public delegate void InboundPacket(VoicePacket packet, EndPoint endPoint);
        public delegate void ExceptionError(Exception error);
        #endregion

        #region Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;
        public event PacketData<Login>? OnLogin;
        public event PacketData<Null>? OnAccept;
        public event PacketData<Deny>? OnDeny;
        public event PacketData<ClientAudio>? OnClientAudio;
        public event PacketData<ServerAudio>? OnServerAudio;
        public event PacketData<UpdatePosition>? OnUpdatePosition;
        public event PacketData<KeepAlive>? OnKeepAlive;
        public event PacketData<Null>? OnNull;

        public event OutboundPacket? OnOutboundPacket;
        public event InboundPacket? OnInboundPacket;
        public event ExceptionError? OnExceptionError;
        #endregion

        #region Methods
        public Voice()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            CTS = new CancellationTokenSource();
        }

        /// <summary>
        /// Connects to a voice socket that is hosting.
        /// </summary>
        /// <param name="IP">IP to connect to.</param>
        /// <param name="Port">The port to connect to.</param>
        /// <param name="LoginKey">The key to login as.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task Connect(string IP, int Port, int privateId)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Voice));
            if (IsConnected) throw new InvalidOperationException("You must disconnect before connecting!");
            if (IsHosting) throw new InvalidOperationException("Cannot connect as the socket is in a hosting state!");

            await Socket.ConnectAsync(IP, Port);

            OnAccept += Accept;
            OnDeny += Deny;
            try
            {
                _ = ListenAsync();
                await SendPacketAsync(Login.Create(privateId));
            }
            catch (Exception ex)
            {
                Disconnect(ex.Message);
            }

            await Task.Delay(5000);
            if (!IsConnected && Socket.Connected)
            {
                Disconnect("Voice timed out.");
            }
        }

        /// <summary>
        /// Starts a server for voice socket connections.
        /// </summary>
        /// <param name="Port">The port to host on.</param>
        public void Host(ushort Port)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Voice));
            if (IsConnected) throw new InvalidOperationException("You must stop hosting before starting a host!");
            if (IsConnected) throw new InvalidOperationException("Cannot start hosting as socket is in a connection state!");

            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
            IsHosting = true;
            _ = ListenAsync();
        }

        /// <summary>
        /// Sends a packet asynchronously, You can send a packet before the voice is connected but not before the UDP socket is connected.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns></returns>
        public async Task SendPacketAsync(VoicePacket packet)
        {
            if (Socket.Connected)
            {
                if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                    OnOutboundPacket?.Invoke(packet, Socket.RemoteEndPoint);

                try
                {
                    var packetStream = packet.GetPacketStream();
                    await Socket.SendAsync(packetStream, SocketFlags.None);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex);
#endif
                    if (LogExceptions)
                    {
                        OnExceptionError?.Invoke(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a packet, You can send a packet before the voice is connected but not before the UDP socket is connected.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendPacket(VoicePacket packet)
        {
            if (Socket.Connected)
            {
                if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                    OnOutboundPacket?.Invoke(packet, Socket.RemoteEndPoint);

                try
                {
                    var packetStream = packet.GetPacketStream();
                    Socket.Send(packetStream, SocketFlags.None);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex);
#endif
                    if (LogExceptions)
                    {
                        OnExceptionError?.Invoke(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a packet asynchronously to an endpoint, You can send a packet before the voice is connected but not before the UDP socket is connected.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns></returns>
        public async Task SendPacketToAsync(VoicePacket packet, EndPoint endPoint)
        {
            if (Socket.Connected || IsHosting)
            {
                if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                    OnOutboundPacket?.Invoke(packet, endPoint);

                try
                {
                    var packetStream = packet.GetPacketStream();
                    await Socket.SendToAsync(packetStream, SocketFlags.None, endPoint);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex);
#endif
                    if (LogExceptions)
                    {
                        OnExceptionError?.Invoke(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Sends a packet to an endpoint, You can send a packet before the voice is connected but not before the UDP socket is connected.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendPacketTo(VoicePacket packet, EndPoint endPoint)
        {
            if (Socket.Connected || IsHosting)
            {
                if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                    OnOutboundPacket?.Invoke(packet, endPoint);

                try
                {
                    var packetStream = packet.GetPacketStream();
                    Socket.SendTo(packetStream, SocketFlags.None, endPoint);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex);
#endif
                    if (LogExceptions)
                    {
                        OnExceptionError?.Invoke(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Disconnects the socket.
        /// </summary>
        /// <param name="reason">The reason that gets passed into the OnDisconnected event.</param>
        public void Disconnect(string? reason = null)
        {
            try
            {
                if (IsHosting) throw new InvalidOperationException("Cannot disconnect as connection is in a hosting state.");

                if (Socket.Connected || IsConnected)
                {
                    IsConnected = false;
                    CTS.Cancel();
                    if (ActivityChecker != null)
                    {
                        _ = Task.Run(() => {
                            ActivityChecker?.Wait(); //Wait to finish before disposing.
                            ActivityChecker?.Dispose();
                            ActivityChecker = null;
                        });
                    }

                    OnDisconnected?.Invoke(reason);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine(ex);
#endif
                if (LogExceptions)
                {
                    OnExceptionError?.Invoke(ex);
                }
            }
        }

        /// <summary>
        /// Stops hosting the voice socket server.
        /// </summary>
        public void StopHosting()
        {
            if (IsConnected) throw new InvalidOperationException("Cannot stop hosting as the socket is in a hosting state.");

            CTS.Cancel();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IsHosting = false;
            OnDisconnected?.Invoke();
        }

        private async Task ListenAsync()
        {
            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];
                    if (IsHosting)
                    {
                        var networkStream = await Socket.ReceiveFromAsync(buffer, SocketFlags.None, IPListener);
                        var packet = new VoicePacket(buffer);

                        if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains(packet.PacketType)))
                            OnInboundPacket?.Invoke(packet, networkStream.RemoteEndPoint);
                        HandlePacket(packet, networkStream.RemoteEndPoint);
                    }
                    else
                    {
                        var networkStream = await Socket.ReceiveFromAsync(buffer, SocketFlags.None, IPListener);
                        var packet = new VoicePacket(buffer);

                        if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains(packet.PacketType)))
                            OnInboundPacket?.Invoke(packet, Socket.RemoteEndPoint);
                        HandlePacket(packet, Socket.RemoteEndPoint);
                    }
                }
                catch (Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);

                    if (CTS.IsCancellationRequested)
                        break;
                }
            }
        }

        private void HandlePacket(VoicePacket packet, EndPoint endPoint)
        {
            if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains(packet.PacketType)))
                OnInboundPacket?.Invoke(packet, endPoint);

            switch (packet.PacketType)
            {
                case VoicePacketTypes.Login: OnLogin?.Invoke((Login)packet.PacketData, endPoint); break;
                case VoicePacketTypes.Accept: OnAccept?.Invoke(new Null(), endPoint); break;
                case VoicePacketTypes.Deny: OnDeny?.Invoke((Deny)packet.PacketData, endPoint); break;
                case VoicePacketTypes.ClientAudio: OnClientAudio?.Invoke((ClientAudio)packet.PacketData, endPoint); break;
                case VoicePacketTypes.ServerAudio: OnServerAudio?.Invoke((ServerAudio)packet.PacketData, endPoint); break;
                case VoicePacketTypes.UpdatePosition: OnUpdatePosition?.Invoke((UpdatePosition)packet.PacketData, endPoint); break;
                case VoicePacketTypes.KeepAlive: OnKeepAlive?.Invoke((KeepAlive)packet.PacketData, endPoint); break;
                default: OnNull?.Invoke(new Null(), endPoint); break;
            }
        }

        private async Task ActivityCheck()
        {
            while (IsConnected)
            {
                await SendPacketAsync(Null.Create(VoicePacketTypes.KeepAlive));
                await Task.Delay(ActivityInterval).ConfigureAwait(false);
            }
        }

        ~Voice()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Disconnect();
                    Socket.Close();
                    IsConnected = false;
                    IsHosting = false;
                    ActivityChecker?.Wait(); //Wait to finish before disposing.
                    ActivityChecker?.Dispose();
                    ActivityChecker = null;
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Event Methods
        private void Accept(Null data, EndPoint endPoint)
        {
            if (!IsConnected)
            {
                IsConnected = true;
                ActivityChecker = Task.Run(async () => await ActivityCheck());
                OnConnected?.Invoke();
            }
        }

        private void Deny(Deny data, EndPoint endPoint)
        {
            if (!IsConnected)
            {
                Disconnect(data.Reason);
            }
        }
        #endregion
    }
}