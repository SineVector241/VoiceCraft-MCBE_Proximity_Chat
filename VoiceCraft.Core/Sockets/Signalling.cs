using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.Signalling;

namespace VoiceCraft.Core.Sockets
{
    public class Signalling : IDisposable
    {
        #region Variables
        private CancellationTokenSource CTS { get; set; }
        public Socket Socket { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsHosting { get; private set; }
        public bool IsDisposed { get; private set; }
        public int LastActive { get; private set; }
        public int ActivityInterval { get; set; } = 1000;
        public int ActivityTimeout { get; set; } = 8000;
        private Task? ActivityChecker { get; set; }

        //Debug Settings
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<SignallingPacketTypes> InboundFilter { get; set; } = new List<SignallingPacketTypes>();
        public List<SignallingPacketTypes> OutboundFilter { get; set; } = new List<SignallingPacketTypes>();
        #endregion

        #region Delegates
        public delegate void Connected(ushort port = 0, ushort key = 0);
        public delegate void Disconnected(string? reason = null);
        public delegate void PacketData<T>(T data, Socket socket);

        public delegate void SocketConnected(Socket socket);
        public delegate void SocketDisconnected(Socket socket, string? reason = null);
        public delegate void OutboundPacket(SignallingPacket packet, Socket socket);
        public delegate void InboundPacket(SignallingPacket packet, Socket socket);
        public delegate void ExceptionError(Exception error);
        #endregion

        #region Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;
        public event PacketData<Login>? OnLogin;
        public event PacketData<Logout>? OnLogout;
        public event PacketData<Accept>? OnAccept;
        public event PacketData<Deny>? OnDeny;
        public event PacketData<BindedUnbinded>? OnBindedUnbinded;
        public event PacketData<DeafenUndeafen>? OnDeafenUndeafen;
        public event PacketData<MuteUnmute>? OnMuteUnmute;
        public event PacketData<AddChannel>? OnAddChannel;
        public event PacketData<JoinLeaveChannel>? OnJoinLeaveChannel;
        public event PacketData<Error>? OnError;
        public event PacketData<Ping>? OnPing;
        public event PacketData<Null>? OnPingCheck;
        public event PacketData<Null>? OnNull;

        public event SocketConnected? OnSocketConnected;
        public event SocketDisconnected? OnSocketDisconnected;
        public event OutboundPacket? OnOutboundPacket;
        public event InboundPacket? OnInboundPacket;
        public event ExceptionError? OnExceptionError;
        #endregion

        #region Methods
        public Signalling()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CTS = new CancellationTokenSource();
        }

        /// <summary>
        /// Connects to a signalling socket that is hosting.
        /// </summary>
        /// <param name="IP">IP to connect to.</param>
        /// <param name="Port">The port to connect to.</param>
        /// <param name="LoginKey">The preferred key to login as.</param>
        /// <param name="PositioningType">The positioning type to connect as.</param>
        /// <param name="Version">Version protocol.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        public async Task Connect(string IP, int Port, ushort LoginKey = 0, PositioningTypes PositioningType = PositioningTypes.ServerSided, string Version = "")
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Voice));
            if (IsConnected) throw new InvalidOperationException("You must disconnect before connecting!");
            if (IsHosting) throw new InvalidOperationException("Cannot connect as the socket is in a hosting state!");

            var cancelTask = Task.Delay(5000);
            var connectTask = Socket.ConnectAsync(IP, Port);
            await await Task.WhenAny(connectTask, cancelTask).ConfigureAwait(false);
            if (cancelTask.IsCompleted) throw new Exception("TCP socket timed out.");

            OnAccept += Accept;
            OnDeny += Deny;

            try
            {
                _ = ListenAsync(Socket);
                await SendPacketAsync(Login.Create(PositioningType, LoginKey, false, false, string.Empty, Version), Socket);
            }
            catch (Exception ex)
            {
                Disconnect(ex.Message);
            }

            await Task.Delay(5000);

            if (!IsConnected && Socket.Connected)
            {
                Disconnect("Signalling timed out.");
            }
        }

        /// <summary>
        /// Starts a server for signalling socket connections.
        /// </summary>
        /// <param name="Port">The port to host on.</param>
        public void Host(ushort Port)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Voice));
            if (IsHosting) throw new InvalidOperationException("You must stop hosting before starting a host!");
            if (IsConnected) throw new InvalidOperationException("Cannot start hosting as socket is in a connection state!");

            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
            Socket.Listen(100);
            IsHosting = true;
            _ = AcceptConnectionsAsync();
        }

        /// <summary>
        /// Sends a packet asynchronously, You can send a packet before the signalling is connected but not before the TCP socket is connected.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns></returns>
        public async Task SendPacketAsync(SignallingPacket packet, Socket socket)
        {
            if (Socket.Connected)
            {
                try
                {
                    var packetStream = packet.GetPacketStream();
                    await socket.SendAsync(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                    await socket.SendAsync(packetStream, SocketFlags.None);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex);
#endif
                }
            }
        }

        /// <summary>
        /// Sends a packet, You can send a packet before the signalling is connected but not before the TCP socket is connected.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendPacket(SignallingPacket packet, Socket socket)
        {
            if (Socket.Connected)
            {
                try
                {
                    var packetStream = packet.GetPacketStream();
                    socket.Send(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                    socket.Send(packetStream, SocketFlags.None);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex);
#endif
                }
            }
        }

        /// <summary>
        /// Disconnects the socket.
        /// </summary>
        /// <param name="reason">The reason that gets passed into the OnDisconnected event.</param>
        /// <param name="force">Whether to force close the socket.</param>
        public void Disconnect(string? reason = null, bool force = false)
        {
            try
            {
                if (IsHosting) throw new InvalidOperationException("Cannot disconnect as connection is in a hosting state.");

                if (Socket.Connected || IsConnected)
                {
                    IsConnected = false;
                    CTS.Cancel();
                    ActivityChecker?.Wait(); //Wait to finish before disposing.
                    ActivityChecker?.Dispose();
                    ActivityChecker = null;
                    OnAccept -= Accept;
                    OnDeny -= Deny;

                    if (force)
                    {
                        Socket.Close();
                        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    }
                    else
                        Socket.Disconnect(true);

                    OnDisconnected?.Invoke(reason);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine(ex);
#endif
            }
        }

        /// <summary>
        /// Stops hosting the signalling socket server.
        /// </summary>
        public void StopHosting()
        {
            if (IsConnected) throw new InvalidOperationException("Cannot stop hosting as the socket is in a hosting state.");

            CTS.Cancel();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IsHosting = false;
            OnDisconnected?.Invoke();
        }

        private async Task ListenAsync(Socket socket)
        {
            byte[]? packetBuffer = null;
            byte[] lengthBuffer = new byte[2];
            var stream = new NetworkStream(socket);

            while (socket.Connected && !CTS.IsCancellationRequested)
            {
                try
                {
                    //TCP Is Annoying
                    var bytes = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length).ConfigureAwait(false);
                    if (bytes == 0)
                    {
                        if (!socket.Connected || CTS.IsCancellationRequested)
                        {
                            if (IsHosting) OnSocketDisconnected?.Invoke(socket, "Client logged out.");
                            break; //Socket is closed.
                        }
                    }

                    ushort packetLength = SignallingPacket.GetPacketLength(lengthBuffer);
                    //If packets are an invalid length then we break out to prevent memory exceptions
                    if (packetLength > 1024)
                    {
                        throw new Exception("Invalid packet received.");
                    }//Packets will never be bigger than 500 bytes but the hard limit is 1024 bytes/1mb

                    packetBuffer = new byte[packetLength];

                    //Read until packet is fully received
                    int offset = 0;
                    while (offset < packetLength)
                    {
                        int bytesRead = await stream.ReadAsync(packetBuffer, offset, packetLength).ConfigureAwait(false);
                        if (bytesRead == 0)
                        {
                            if (IsHosting) OnSocketDisconnected?.Invoke(socket, "Client logged out.");
                            break; //Socket is closed.
                        }

                        offset += bytesRead;
                    }
                    var packet = new SignallingPacket(packetBuffer);
                    HandlePacket(packet, socket);
                }
                catch (SocketException ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);

                    if (!socket.Connected || CTS.IsCancellationRequested || ex.ErrorCode == 995) //Break out and dispose if its an IO exception or if TCP is not connected or disconnect requested.
                    {
                        if (IsHosting)
                            OnSocketDisconnected?.Invoke(socket, "Lost connection.");
                        else
                            Disconnect(ex.Message);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);

                    if (!socket.Connected || CTS.IsCancellationRequested)
                    {
                        if (IsHosting)
                            OnSocketDisconnected?.Invoke(socket, ex.Message);
                        else
                            Disconnect(ex.Message);
                        break;
                    }
                }
            }

            await stream.DisposeAsync();
            if (IsHosting)
                socket.Close();
            else
                Disconnect();
        }

        private async Task AcceptConnectionsAsync()
        {
            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var socket = await Socket.AcceptAsync();
                    OnSocketConnected?.Invoke(socket);
                    _ = Task.Run(async () =>
                    {
                        await ListenAsync(socket);
                    });
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

        private void HandlePacket(SignallingPacket packet, Socket socket)
        {
            LastActive = Environment.TickCount;
            switch (packet.PacketType)
            {
                case SignallingPacketTypes.Login: OnLogin?.Invoke((Login)packet.PacketData, socket); break;
                case SignallingPacketTypes.Logout: OnLogout?.Invoke((Logout)packet.PacketData, socket); break;
                case SignallingPacketTypes.Accept: OnAccept?.Invoke((Accept)packet.PacketData, socket); break;
                case SignallingPacketTypes.Deny: OnDeny?.Invoke((Deny)packet.PacketData, socket); break;
                case SignallingPacketTypes.BindedUnbinded: OnBindedUnbinded?.Invoke((BindedUnbinded)packet.PacketData, socket); break;
                case SignallingPacketTypes.DeafenUndeafen: OnDeafenUndeafen?.Invoke((DeafenUndeafen)packet.PacketData, socket); break;
                case SignallingPacketTypes.MuteUnmute: OnMuteUnmute?.Invoke((MuteUnmute)packet.PacketData, socket); break;
                case SignallingPacketTypes.AddChannel: OnAddChannel?.Invoke((AddChannel)packet.PacketData, socket); break;
                case SignallingPacketTypes.JoinLeaveChannel: OnJoinLeaveChannel?.Invoke((JoinLeaveChannel)packet.PacketData, socket); break;
                case SignallingPacketTypes.Error: OnError?.Invoke((Error)packet.PacketData, socket); break;
                case SignallingPacketTypes.Ping: OnPing?.Invoke((Ping)packet.PacketData, socket); break;
                case SignallingPacketTypes.PingCheck: OnPingCheck?.Invoke((Null)packet.PacketData, socket); break;
                default: OnNull?.Invoke(new Null(), socket); break;
            };
        }

        private async Task ActivityCheck()
        {
            while (IsConnected)
            {
                var dist = Environment.TickCount - (long)LastActive; //negative distance wraps
                if (dist > ActivityTimeout)
                {
                    Disconnect("Signalling timed out!", true);
                    break;
                }
                await SendPacketAsync(Null.Create(SignallingPacketTypes.PingCheck), Socket);
                await Task.Delay(ActivityTimeout).ConfigureAwait(false);
            }
        }

        ~Signalling()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Socket.Close();
                    IsConnected = false;
                    IsHosting = false;
                    if (ActivityChecker != null)
                    {
                        ActivityChecker.Wait(); //Wait to finish before disposing.
                        ActivityChecker.Dispose();
                        ActivityChecker = null;
                    }
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
        private void Accept(Accept data, Socket socket)
        {
            if (!IsConnected)
            {
                IsConnected = true;
                LastActive = Environment.TickCount;
                ActivityChecker = Task.Run(async () => await ActivityCheck());
                OnConnected?.Invoke(data.VoicePort, data.Key);
            }
        }

        private void Deny(Deny data, Socket socket)
        {
            if (!IsConnected)
            {
                Disconnect(data.Reason, true);
            }
        }
        #endregion
    }
}
