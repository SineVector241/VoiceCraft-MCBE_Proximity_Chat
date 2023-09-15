using System.Net;
using System.Net.Sockets;
using System.Threading;
using VoiceCraft.Network.Packets;
using VoiceCraft.Network.Packets.Voice;

namespace VoiceCraft.Network.Sockets.Server
{
    public class VoiceSocket
    {
        public Socket UDPSocket { get; }
        public CancellationToken CTS { get; }
        public IPEndPoint IPListener { get; } = new IPEndPoint(IPAddress.Any, 0);

        //Delegates
        public delegate void LoginPacket(Login packet);
        public delegate void AcceptPacket(Accept packet);
        public delegate void DenyPacket(Deny packet);
        public delegate void ClientAudioPacket(ClientAudio packet);
        public delegate void ServerAudioPacket(ServerAudio packet);
        public delegate void UpdatePositionPacket(UpdatePosition packet);
        public delegate void NullPacket(Null packet);

        //Events
        public event LoginPacket? OnLoginPacketReceived;
        public event AcceptPacket? OnAcceptPacketReceived;
        public event DenyPacket? OnDenyPacketReceived;
        public event ClientAudioPacket? OnClientAudioPacketReceived;
        public event ServerAudioPacket? OnServerAudioPacketReceived;
        public event UpdatePositionPacket? OnUpdatePositionPacketReceived;
        public event NullPacket? OnNullPacketReceived;

        public VoiceSocket(CancellationToken Token)
        {
            UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            CTS = Token;
        }

        public void Start(int Port)
        {
            UDPSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            ListenAsync();
        }

        private async void ListenAsync()
        {
            while (UDPSocket.Connected && !CTS.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];
                    var networkStream = await UDPSocket.ReceiveFromAsync(buffer, SocketFlags.None, IPListener);
                    var packet = new VoicePacket(buffer);
                    HandlePacket(packet);
                }
                catch
                {
                    if (!UDPSocket.Connected && !CTS.IsCancellationRequested)
                        break;
                }
            }
        }

        private void HandlePacket(VoicePacket packet)
        {
            switch (packet.PacketType)
            {
                case VoicePacketTypes.Login:
                    OnLoginPacketReceived?.Invoke((Login)packet.PacketData);
                    break;
                case VoicePacketTypes.Accept:
                    OnAcceptPacketReceived?.Invoke((Accept)packet.PacketData);
                    break;
                case VoicePacketTypes.Deny:
                    OnDenyPacketReceived?.Invoke((Deny)packet.PacketData);
                    break;
                case VoicePacketTypes.ClientAudio:
                    OnClientAudioPacketReceived?.Invoke((ClientAudio)packet.PacketData);
                    break;
                case VoicePacketTypes.ServerAudio:
                    OnServerAudioPacketReceived?.Invoke((ServerAudio)packet.PacketData);
                    break;
                case VoicePacketTypes.UpdatePosition:
                    OnUpdatePositionPacketReceived?.Invoke((UpdatePosition)packet.PacketData);
                    break;
                case VoicePacketTypes.Null:
                    OnNullPacketReceived?.Invoke((Null)packet.PacketData);
                    break;
            }
        }
    }
}
