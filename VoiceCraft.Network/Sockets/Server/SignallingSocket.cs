using System.Net;
using System.Net.Sockets;
using System.Threading;
using VoiceCraft.Network.Packets;
using VoiceCraft.Network.Packets.Signalling;

namespace VoiceCraft.Network.Sockets.Server
{
    public class SignallingSocket
    {
        public Socket TCPSocket { get; }
        public CancellationToken CTS { get; }
        public IPEndPoint IPListener { get; } = new IPEndPoint(IPAddress.Any, 0);

        //Delegates
        public delegate void LoginPacket(Login packet);
        public delegate void LogoutPacket(Logout packet);
        public delegate void AcceptPacket(Accept packet);
        public delegate void DenyPacket(Deny packet);
        public delegate void BindedPacket(Binded packet);
        public delegate void DeafenPacket(Deafen packet);
        public delegate void UndeafenPacket(Undeafen packet);
        public delegate void MutePacket(Mute packet);
        public delegate void UnmutePacket(Unmute packet);
        public delegate void ErrorPacket(Error packet);
        public delegate void PingPacket(Ping packet);
        public delegate void NullPacket(Null packet);

        //Events
        public event LoginPacket? OnLoginPacketReceived;
        public event LogoutPacket? OnLogoutPacketReceived;
        public event AcceptPacket? OnAcceptPacketReceived;
        public event DenyPacket? OnDenyPacketReceived;
        public event BindedPacket? OnBindedPacketReceived;
        public event DeafenPacket? OnDeafenPacketReceived;
        public event UndeafenPacket? OnUndeafenPacketReceived;
        public event MutePacket? OnMutePacketReceived;
        public event UnmutePacket? OnUnmutePacketReceived;
        public event ErrorPacket? OnErrorPacketReceived;
        public event PingPacket? OnPingPacketReceived;
        public event NullPacket? OnNullPacketReceived;


        public SignallingSocket(CancellationToken Token)
        {
            TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CTS = Token;
        }

        public void Start(int Port)
        {
            TCPSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            ListenAsync();
        }

        private async void ListenAsync()
        {
            while(TCPSocket.Connected && !CTS.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];
                    var networkStream = await TCPSocket.ReceiveFromAsync(buffer, SocketFlags.None, IPListener);
                    var packet = new SignallingPacket(buffer);
                    HandlePacket(packet);
                }
                catch
                {
                    if(!TCPSocket.Connected && !CTS.IsCancellationRequested)
                        break;
                }
            }
        }

        private void HandlePacket(SignallingPacket packet)
        {
            switch(packet.PacketType)
            {
                case SignallingPacketTypes.Login:
                    OnLoginPacketReceived?.Invoke((Login)packet.PacketData);
                    break;
                case SignallingPacketTypes.Logout:
                    OnLogoutPacketReceived?.Invoke((Logout)packet.PacketData);
                    break;
                case SignallingPacketTypes.Accept:
                    OnAcceptPacketReceived?.Invoke((Accept)packet.PacketData);
                    break;
                case SignallingPacketTypes.Deny:
                    OnDenyPacketReceived?.Invoke((Deny)packet.PacketData);
                    break;
                case SignallingPacketTypes.Binded:
                    OnBindedPacketReceived?.Invoke((Binded)packet.PacketData);
                    break;
                case SignallingPacketTypes.Deafen:
                    OnDeafenPacketReceived?.Invoke((Deafen)packet.PacketData);
                    break;
                case SignallingPacketTypes.Undeafen:
                    OnUndeafenPacketReceived?.Invoke((Undeafen)packet.PacketData);
                    break;
                case SignallingPacketTypes.Mute:
                    OnMutePacketReceived?.Invoke((Mute)packet.PacketData); 
                    break;
                case SignallingPacketTypes.Unmute:
                    OnMutePacketReceived?.Invoke((Mute)packet.PacketData);
                    break;
                case SignallingPacketTypes.Error:
                    OnErrorPacketReceived?.Invoke((Error)packet.PacketData);
                    break;
                case SignallingPacketTypes.Ping:
                    OnPingPacketReceived?.Invoke((Ping)packet.PacketData);
                    break;
                case SignallingPacketTypes.Null:
                    OnNullPacketReceived?.Invoke((Null)packet.PacketData);
                    break;
            }
        }
    }
}
