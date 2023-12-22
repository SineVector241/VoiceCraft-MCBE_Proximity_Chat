using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.Signalling;

namespace VoiceCraft.Core.Server.Sockets
{
    public class SignallingSocket
    {
        public Socket TCPSocket { get; }
        public CancellationToken CT { get; }
        public IPEndPoint IPListener { get; } = new IPEndPoint(IPAddress.Any, 0);
        
        //Debug Settings
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<SignallingPacketTypes> InboundFilter { get; set; } = new List<SignallingPacketTypes>();
        public List<SignallingPacketTypes> OutboundFilter { get; set; } = new List<SignallingPacketTypes>();
        public Dictionary<Socket, NetworkStream> ConnectedSockets { get; set; } = new Dictionary<Socket, NetworkStream>();

        //Delegates
        public delegate void Started();
        public delegate void LoginPacket(Login packet, Socket socket);
        public delegate void LogoutPacket(Logout packet, Socket socket);
        public delegate void AcceptPacket(Accept packet, Socket socket);
        public delegate void DenyPacket(Deny packet, Socket socket);
        public delegate void BindedPacket(Binded packet, Socket socket);
        public delegate void UnbindedPacket(Unbinded packet, Socket socket);
        public delegate void DeafenPacket(Deafen packet, Socket socket);
        public delegate void UndeafenPacket(Undeafen packet, Socket socket);
        public delegate void MutePacket(Mute packet, Socket socket);
        public delegate void UnmutePacket(Unmute packet, Socket socket);
        public delegate void ErrorPacket(Error packet, Socket socket);
        public delegate void PingPacket(Ping packet, Socket socket);
        public delegate void PingCheckPacket(PingCheck packet, Socket socket);
        public delegate void NullPacket(Null packet, Socket socket);

        public delegate void SocketConnected(Socket socket, NetworkStream stream);
        public delegate void SocketDisconnected(Socket socket, string reason);
        public delegate void OutboundPacket(ISignallingPacket packet, Socket socket);
        public delegate void InboundPacket(ISignallingPacket packet, Socket socket);
        public delegate void ExceptionError(Exception error);

        //Events
        public event Started? OnStarted;
        public event LoginPacket? OnLoginPacketReceived;
        public event LogoutPacket? OnLogoutPacketReceived;
        public event AcceptPacket? OnAcceptPacketReceived;
        public event DenyPacket? OnDenyPacketReceived;
        public event BindedPacket? OnBindedPacketReceived;
        public event UnbindedPacket? OnUnbindedPacketReceived;
        public event DeafenPacket? OnDeafenPacketReceived;
        public event UndeafenPacket? OnUndeafenPacketReceived;
        public event MutePacket? OnMutePacketReceived;
        public event UnmutePacket? OnUnmutePacketReceived;
        public event ErrorPacket? OnErrorPacketReceived;
        public event PingPacket? OnPingPacketReceived;
        public event PingCheckPacket? OnPingCheckPacketReceived;
        public event NullPacket? OnNullPacketReceived;

        public event SocketConnected? OnSocketConnected;
        public event SocketDisconnected? OnSocketDisconnected;
        public event OutboundPacket? OnOutboundPacket;
        public event InboundPacket? OnInboundPacket;
        public event ExceptionError? OnExceptionError;


        public SignallingSocket(CancellationToken CT)
        {
            TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.CT = CT;
        }

        public void Start(ushort Port)
        {
            TCPSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            TCPSocket.Listen(100);
            AcceptConnectionsAsync();
            OnStarted?.Invoke();
        }

        public async void SendPacketAsync(ISignallingPacket packet, Socket socket)
        {
            try
            {
                if (socket.Connected)
                {
                    var packetStream = packet.GetPacketStream();
                    await socket.SendAsync(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                    await socket.SendAsync(packetStream, SocketFlags.None);

                    if(LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                        OnOutboundPacket?.Invoke(packet, socket);
                }
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);
            }
        }

        public void SendPacket(ISignallingPacket packet, Socket socket)
        {
            try
            {
                if (socket.Connected)
                {
                    var packetStream = packet.GetPacketStream();
                    socket.Send(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                    socket.Send(packetStream, SocketFlags.None);

                    if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                        OnOutboundPacket?.Invoke(packet, socket);
                }
            }
            catch(Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);
            }
        }

        public void Stop()
        {
            TCPSocket.Close();
            TCPSocket.Dispose();
        }

        private async void ListenAsync(Socket socket, NetworkStream stream)
        {
            byte[]? packetBuffer = null;
            byte[] lengthBuffer = new byte[2];
            while (!CT.IsCancellationRequested)
            {
                try
                {
                    //TCP Is Annoying
                    var bytes = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length).ConfigureAwait(false);
                    if (bytes == 0) break;

                    ushort packetLength = SignallingPacket.GetPacketLength(lengthBuffer);
                    //If packets are an invalid length then we break out to prevent memory exceptions and disconnect the client.
                    if (packetLength > 1024)
                    {
                        socket.Close(); //Close the socket because the client sent an invalid packet.
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

                    if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains(packet.PacketType)))
                        OnInboundPacket?.Invoke(packet, socket);
                    HandlePacket(packet, socket);
                }
                catch(IOException ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);

                    if (!socket.Connected || CT.IsCancellationRequested)
                    {
                        OnSocketDisconnected?.Invoke(socket, "Lost connection.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);

                    if (!socket.Connected || CT.IsCancellationRequested)
                    {
                        OnSocketDisconnected?.Invoke(socket, ex.Message);
                        break;
                    }
                }
            }

            await stream.DisposeAsync();
            socket.Close();
            ConnectedSockets.Remove(socket);
            OnSocketDisconnected?.Invoke(socket, "Client logged out");
        }

        private async void AcceptConnectionsAsync()
        {
            while (!CT.IsCancellationRequested)
            {
                try
                {
                    var socket = await TCPSocket.AcceptAsync();
                    var stream = new NetworkStream(socket);
                    ConnectedSockets.Add(socket, stream);
                    OnSocketConnected?.Invoke(socket, stream);
                    ListenAsync(socket, stream);
                }
                catch(Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);

                    if (CT.IsCancellationRequested)
                        break;
                }
            }
        }

        private void HandlePacket(SignallingPacket packet, Socket socket)
        {
            switch (packet.PacketType)
            {
                case SignallingPacketTypes.Login:
                    OnLoginPacketReceived?.Invoke((Login)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Logout:
                    OnLogoutPacketReceived?.Invoke((Logout)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Accept:
                    OnAcceptPacketReceived?.Invoke((Accept)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Deny:
                    OnDenyPacketReceived?.Invoke((Deny)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Binded:
                    OnBindedPacketReceived?.Invoke((Binded)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Unbinded:
                    OnUnbindedPacketReceived?.Invoke((Unbinded)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Deafen:
                    OnDeafenPacketReceived?.Invoke((Deafen)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Undeafen:
                    OnUndeafenPacketReceived?.Invoke((Undeafen)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Mute:
                    OnMutePacketReceived?.Invoke((Mute)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Unmute:
                    OnUnmutePacketReceived?.Invoke((Unmute)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Error:
                    OnErrorPacketReceived?.Invoke((Error)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Ping:
                    OnPingPacketReceived?.Invoke((Ping)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.PingCheck:
                    OnPingCheckPacketReceived?.Invoke((PingCheck)packet.PacketData, socket);
                    break;
                case SignallingPacketTypes.Null:
                    OnNullPacketReceived?.Invoke((Null)packet.PacketData, socket);
                    break;
            }
        }
    }
}
