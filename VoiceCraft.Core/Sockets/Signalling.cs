using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.Signalling;

namespace VoiceCraft.Core.Sockets
{
    public class Signalling
    {
        #region Variables
        private CancellationTokenSource CTS { get; set; }
        public Socket Socket { get; private set; }
        public bool IsConnected { get; private set; }

        //Debug Settings
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<SignallingPacketTypes> InboundFilter { get; set; } = new List<SignallingPacketTypes>();
        public List<SignallingPacketTypes> OutboundFilter { get; set; } = new List<SignallingPacketTypes>();
        #endregion

        #region Delegates
        public delegate void Connected();
        public delegate void Disconnected(string? reason = null);

        public delegate void SocketConnected(Socket socket);
        public delegate void SocketDisconnected(Socket socket);
        public delegate void OutboundPacket(SignallingPacket packet, Socket socket);
        public delegate void InboundPacket(SignallingPacket packet, Socket socket);
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

        public Signalling()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CTS = new CancellationTokenSource();
        }

        public async Task Connect(string IP, int Port, ushort LoginKey = 0, PositioningTypes PositioningType = PositioningTypes.ServerSided, string Version = "")
        {
            if (IsConnected) throw new InvalidOperationException("You must disconnect before connecting!");

            var cancelTask = Task.Delay(5000);
            var connectTask = Socket.ConnectAsync(IP, Port);
            await await Task.WhenAny(connectTask, cancelTask).ConfigureAwait(false);
            if (cancelTask.IsCompleted) throw new Exception("TCP socket timed out.");

            try
            {
                _ = ListenAsync(Socket);
                await SendPacketAsync(Login.Create(PositioningType, LoginKey, false, false, string.Empty, Version));
                await Task.Delay(5000);
                
                if (!IsConnected) throw new Exception("Signalling timed out.");
                Disconnect();
            }
            catch (Exception ex)
            {
                Disconnect();
                throw ex;
            }
        }

        public void Host(ushort Port)
        {
            Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
            Socket.Listen(100);
            _ = AcceptConnectionsAsync();
            OnConnected?.Invoke();
        }

        public async Task SendPacketAsync(ISignallingPacket packet)
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

        public void SendPacket(ISignallingPacket packet)
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

        public void Disconnect(string? reason = null, bool force = false)
        {
            try
            {
                if (IsConnected)
                {
                    CTS.Cancel();
                    if (force)
                    {
                        Socket.Close();
                        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    }
                    else
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

        public void StopHosting()
        {
            CTS.Cancel();
            Socket.Close();
            Socket.Dispose();
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
                            break;
                    }//Socket is closed.

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
                        if (bytesRead == 0) break; //Socket is closed.

                        offset += bytesRead;
                    }
                    var packet = new SignallingPacket(packetBuffer);
                    HandlePacket(packet);
                }
                catch (SocketException ex)
                {
                    if (!socket.Connected || CTS.IsCancellationRequested || ex.ErrorCode == 995) //Break out and dispose if its an IO exception or if TCP is not connected or disconnect requested.
                    {
                        Disconnect(ex.Message);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (!socket.Connected || CTS.IsCancellationRequested)
                    {
                        Disconnect(ex.Message);
                        break;
                    }
                }
            }

            await stream.DisposeAsync();
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

        private void HandlePacket(SignallingPacket packet)
        {

        }
    }
}
