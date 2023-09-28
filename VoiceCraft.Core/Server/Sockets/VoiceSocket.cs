using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.Voice;

namespace VoiceCraft.Core.Server.Sockets
{
    public class VoiceSocket
    {
        public Socket UDPSocket { get; }
        public CancellationToken CT { get; }
        public IPEndPoint IPListener { get; } = new IPEndPoint(IPAddress.Any, 0);

        //Delegates
        public delegate void Started();
        public delegate void LoginPacket(Login packet, EndPoint endPoint);
        public delegate void AcceptPacket(Accept packet, EndPoint endPoint);
        public delegate void DenyPacket(Deny packet, EndPoint endPoint);
        public delegate void ClientAudioPacket(ClientAudio packet, EndPoint endPoint);
        public delegate void ServerAudioPacket(ServerAudio packet, EndPoint endPoint);
        public delegate void UpdatePositionPacket(UpdatePosition packet, EndPoint endPoint);
        public delegate void NullPacket(Null packet, EndPoint endPoint);

        //Events
        public event Started? OnStarted;
        public event LoginPacket? OnLoginPacketReceived;
        public event AcceptPacket? OnAcceptPacketReceived;
        public event DenyPacket? OnDenyPacketReceived;
        public event ClientAudioPacket? OnClientAudioPacketReceived;
        public event ServerAudioPacket? OnServerAudioPacketReceived;
        public event UpdatePositionPacket? OnUpdatePositionPacketReceived;
        public event NullPacket? OnNullPacketReceived;

        public VoiceSocket(CancellationToken CT)
        {
            UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.CT = CT;
        }

        public void Start(ushort Port)
        {
            UDPSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            ListenAsync();
            OnStarted?.Invoke();
        }

        public async void SendPacketAsync(IVoicePacket packet, EndPoint EP)
        {
            await UDPSocket.SendToAsync(packet.GetPacketStream(), SocketFlags.None, EP);
        }

        public void SendPacket(IVoicePacket packet, EndPoint EP)
        {
            UDPSocket.SendTo(packet.GetPacketStream(), SocketFlags.None, EP);
        }

        public void Stop()
        {
            UDPSocket.Close();
            UDPSocket.Dispose();
        }

        private async void ListenAsync()
        {
            while (!CT.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];
                    var networkStream = await UDPSocket.ReceiveFromAsync(buffer, SocketFlags.None, IPListener);
                    var packet = new VoicePacket(buffer);
                    HandlePacket(packet, networkStream.RemoteEndPoint);
                }
                catch
                {
                    if (CT.IsCancellationRequested)
                        break;
                }
            }
        }

        private void HandlePacket(VoicePacket packet, EndPoint EP)
        {
            switch (packet.PacketType)
            {
                case VoicePacketTypes.Login:
                    OnLoginPacketReceived?.Invoke((Login)packet.PacketData, EP);
                    break;
                case VoicePacketTypes.Accept:
                    OnAcceptPacketReceived?.Invoke((Accept)packet.PacketData, EP);
                    break;
                case VoicePacketTypes.Deny:
                    OnDenyPacketReceived?.Invoke((Deny)packet.PacketData, EP);
                    break;
                case VoicePacketTypes.ClientAudio:
                    OnClientAudioPacketReceived?.Invoke((ClientAudio)packet.PacketData, EP);
                    break;
                case VoicePacketTypes.ServerAudio:
                    OnServerAudioPacketReceived?.Invoke((ServerAudio)packet.PacketData, EP);
                    break;
                case VoicePacketTypes.UpdatePosition:
                    OnUpdatePositionPacketReceived?.Invoke((UpdatePosition)packet.PacketData, EP);
                    break;
                case VoicePacketTypes.Null:
                    OnNullPacketReceived?.Invoke((Null)packet.PacketData, EP);
                    break;
            }
        }
    }
}
