using System;
using System.Collections.Generic;
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

        //Debug Settings
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<VoicePacketTypes> InboundFilter { get; set; } = new List<VoicePacketTypes>();
        public List<VoicePacketTypes> OutboundFilter { get; set; } = new List<VoicePacketTypes>();

        //Delegates
        public delegate void Started();
        public delegate void LoginPacket(Login packet, EndPoint endPoint);
        public delegate void AcceptPacket(Accept packet, EndPoint endPoint);
        public delegate void DenyPacket(Deny packet, EndPoint endPoint);
        public delegate void ClientAudioPacket(ClientAudio packet, EndPoint endPoint);
        public delegate void ServerAudioPacket(ServerAudio packet, EndPoint endPoint);
        public delegate void UpdatePositionPacket(UpdatePosition packet, EndPoint endPoint);
        public delegate void NullPacket(Null packet, EndPoint endPoint);

        public delegate void OutboundPacket(IVoicePacket packet, EndPoint endPoint);
        public delegate void InboundPacket(IVoicePacket packet, EndPoint endPoint);
        public delegate void ExceptionError(Exception error);

        //Events
        public event Started? OnStarted;
        public event LoginPacket? OnLoginPacketReceived;
        public event AcceptPacket? OnAcceptPacketReceived;
        public event DenyPacket? OnDenyPacketReceived;
        public event ClientAudioPacket? OnClientAudioPacketReceived;
        public event ServerAudioPacket? OnServerAudioPacketReceived;
        public event UpdatePositionPacket? OnUpdatePositionPacketReceived;
        public event NullPacket? OnNullPacketReceived;

        public event OutboundPacket? OnOutboundPacket;
        public event InboundPacket? OnInboundPacket;
        public event ExceptionError? OnExceptionError;

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
            try
            {
                await UDPSocket.SendToAsync(packet.GetPacketStream(), SocketFlags.None, EP);

                if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                    OnOutboundPacket?.Invoke(packet, EP);
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);
            }
        }

        public void SendPacket(IVoicePacket packet, EndPoint EP)
        {
            try
            {
                UDPSocket.SendTo(packet.GetPacketStream(), SocketFlags.None, EP);

                if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                    OnOutboundPacket?.Invoke(packet, EP);
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);
            }
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

                    if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains(packet.PacketType)))
                        OnInboundPacket?.Invoke(packet, networkStream.RemoteEndPoint);
                    HandlePacket(packet, networkStream.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);

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
