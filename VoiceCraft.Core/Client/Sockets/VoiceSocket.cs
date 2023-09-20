using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.Voice;

namespace VoiceCraft.Core.Client.Sockets
{
    public class VoiceSocket
    {
        public Socket UDPSocket { get; }
        public CancellationToken CTS { get; }

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

        public void Connect(string IP, int Port)
        {
            UDPSocket.Connect(IP, Port);
            ListenAsync();
        }

        public async void SendPacketAsync(IVoicePacket packet)
        {
            await UDPSocket.SendAsync(packet.GetPacketStream(), SocketFlags.None);
        }

        public void SendPacket(IVoicePacket packet)
        {
            UDPSocket.Send(packet.GetPacketStream(), SocketFlags.None);
        }

        public void Disconnect()
        {
            try
            {
                if (UDPSocket.Connected)
                    UDPSocket.Close();
                UDPSocket.Dispose();
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine(ex);
#endif
            }
        }

        private async void ListenAsync()
        {
            while (UDPSocket.Connected && !CTS.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];
                    var networkStream = await UDPSocket.ReceiveAsync(buffer, SocketFlags.None);
                    var packet = new VoicePacket(buffer);
                    HandlePacket(packet);
                }
                catch
                {
                    if (!UDPSocket.Connected && !CTS.IsCancellationRequested)
                    {
                        Disconnect();
                        break;
                    }
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
