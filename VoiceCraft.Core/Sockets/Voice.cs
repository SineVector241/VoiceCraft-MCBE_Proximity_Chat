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
        public bool IsDisposed { get; private set; }
        public IPEndPoint IPListener { get; } = new IPEndPoint(IPAddress.Any, 0);

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

        public delegate void SocketConnected(Socket socket);
        public delegate void SocketDisconnected(Socket socket);
        public delegate void OutboundPacket(VoicePacket packet, EndPoint socket);
        public delegate void InboundPacket(VoicePacket packet, EndPoint socket);
        public delegate void ExceptionError(Exception error);
        #endregion

        #region Events
        public event Connected? OnConnected;
        public event Disconnected? OnDisconnected;

        public event SocketConnected? OnSocketConnected;
        public event SocketDisconnected? OnSocketDisconnected;
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
        public async Task Connect(string IP, int Port, ushort LoginKey = 0)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(Voice));
            if (IsConnected) throw new InvalidOperationException("You must disconnect before connecting!");

            await Socket.ConnectAsync(IP, Port);
            try
            {
                _ = ListenAsync();
                await SendPacketAsync(Login.Create(LoginKey));
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

            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
            _ = ListenAsync();
            IsConnected = true;
            OnConnected?.Invoke();
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
                try
                {
                    var packetStream = packet.GetPacketStream();
                    await Socket.SendAsync(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                    await Socket.SendAsync(packetStream, SocketFlags.None);
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
        /// Sends a packet, You can send a packet before the voice is connected but not before the UDP socket is connected.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public void SendPacket(VoicePacket packet)
        {
            if (Socket.Connected)
            {
                try
                {
                    var packetStream = packet.GetPacketStream();
                    Socket.Send(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                    Socket.Send(packetStream, SocketFlags.None);
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
        public void Disconnect(string? reason = null)
        {
            try
            {
                if (Socket.Connected)
                {
                    CTS.Cancel();
                    Socket.Disconnect(true);

                    OnDisconnected?.Invoke(reason);
                    IsConnected = false;
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
        /// Stops hosting the voice socket server.
        /// </summary>
        public void StopHosting()
        {
            CTS.Cancel();
            Socket.Close();
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IsConnected = false;
            OnDisconnected?.Invoke();
        }

        private async Task ListenAsync()
        {
            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];
                    var networkStream = await Socket.ReceiveFromAsync(buffer, SocketFlags.None, IPListener);
                    var packet = new VoicePacket(buffer);

                    if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains(packet.PacketType)))
                        OnInboundPacket?.Invoke(packet, networkStream.RemoteEndPoint);
                    HandlePacket(packet, networkStream.RemoteEndPoint);
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

        }
        #endregion
    }
}
