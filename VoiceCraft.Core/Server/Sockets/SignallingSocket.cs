using System;
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
        public CancellationToken CTS { get; }
        public IPEndPoint IPListener { get; } = new IPEndPoint(IPAddress.Any, 0);

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
        public delegate void NullPacket(Null packet, Socket socket);

        public delegate void SocketDisconnected(Socket socket, string reason);

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
        public event NullPacket? OnNullPacketReceived;

        public event SocketDisconnected? OnSocketDisconnected;


        public SignallingSocket(CancellationToken Token)
        {
            TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CTS = Token;
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
            await socket.SendAsync(packet.GetPacketStream(), SocketFlags.None);
        }

        public void SendPacket(ISignallingPacket packet, Socket socket)
        {
            socket.Send(packet.GetPacketStream(), SocketFlags.None);
        }

        public void Stop()
        {
            TCPSocket.Close();
            TCPSocket.Dispose();
        }

        private async void ListenAsync(Socket socket)
        {
            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    CTS.ThrowIfCancellationRequested();
                    var buffer = new byte[1024];
                    var networkStream = await socket.ReceiveAsync(buffer, SocketFlags.None);
                    var packet = new SignallingPacket(buffer);
                    HandlePacket(packet, socket);
                }
                catch (Exception ex)
                {
                    if (!socket.Connected || CTS.IsCancellationRequested)
                    {
                        OnSocketDisconnected?.Invoke(socket, ex.Message);
                        break;
                    }
                }
            }
        }

        private async void AcceptConnectionsAsync()
        {
            while (!CTS.IsCancellationRequested)
            {
                try
                {
                    var handle = await TCPSocket.AcceptAsync();
                    ListenAsync(handle);
                }
                catch
                {
                    if (CTS.IsCancellationRequested)
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
                case SignallingPacketTypes.Null:
                    OnNullPacketReceived?.Invoke((Null)packet.PacketData, socket);
                    break;
            }
        }
    }
}
