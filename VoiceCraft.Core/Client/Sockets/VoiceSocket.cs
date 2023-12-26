using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.Voice;

namespace VoiceCraft.Core.Client.Sockets
{
    public class VoiceSocket : IDisposable
    {
        public Socket UDPSocket { get; }
        public CancellationTokenSource CTS { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsDisposed { get; private set; }

        //Delegates
        public delegate void LoginPacket(Login packet);
        public delegate void AcceptPacket(Accept packet);
        public delegate void DenyPacket(Deny packet);
        public delegate void ClientAudioPacket(ClientAudio packet);
        public delegate void ServerAudioPacket(ServerAudio packet);
        public delegate void UpdatePositionPacket(UpdatePosition packet);
        public delegate void NullPacket(Null packet);

        public delegate void SocketDisconnected(string reason);

        //Events
        public event LoginPacket? OnLoginPacketReceived;
        public event AcceptPacket? OnAcceptPacketReceived;
        public event DenyPacket? OnDenyPacketReceived;
        public event ClientAudioPacket? OnClientAudioPacketReceived;
        public event ServerAudioPacket? OnServerAudioPacketReceived;
        public event UpdatePositionPacket? OnUpdatePositionPacketReceived;
        public event NullPacket? OnNullPacketReceived;

        public event SocketDisconnected? OnSocketDisconnected;

        public VoiceSocket()
        {
            UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            CTS = new CancellationTokenSource();
        }

        public async Task ConnectAsync(string IP, int Port, ushort LoginKey = 0)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(SignallingSocket));
            if (IsConnected) throw new InvalidOperationException("You must disconnect before connecting!");

            try
            {
                UDPSocket.Connect(IP, Port);
                _ = ListenAsync();
                await SendPacketAsync(Login.Create(LoginKey));
                await Task.Delay(5000);
                if (!IsConnected) throw new Exception("Voice timed out");
            }
            catch (Exception ex)
            {
                Disconnect(ex.Message);
            }
        }

        public async Task SendPacketAsync(IVoicePacket packet)
        {
            if(!CTS.IsCancellationRequested)
                await UDPSocket.SendAsync(packet.GetPacketStream(), SocketFlags.None);
        }

        public void SendPacket(IVoicePacket packet)
        {
            if (!CTS.IsCancellationRequested)
                UDPSocket.Send(packet.GetPacketStream(), SocketFlags.None);
        }

        public void Disconnect(string? reason = null)
        {
            try
            {
                CTS.Cancel();
                if (!string.IsNullOrWhiteSpace(reason)) OnSocketDisconnected?.Invoke(reason);
                IsConnected = false;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine(ex);
#endif
            }
        }

        private async Task ListenAsync()
        {
            while (!CTS.IsCancellationRequested)
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
                    if (CTS.IsCancellationRequested)
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
                    if (IsConnected)
                        OnLoginPacketReceived?.Invoke((Login)packet.PacketData);
                    break;
                case VoicePacketTypes.Accept:
                    IsConnected = true;
                    OnAcceptPacketReceived?.Invoke((Accept)packet.PacketData);
                    break;
                case VoicePacketTypes.Deny:
                    var packetData = (Deny)packet.PacketData;
                    OnDenyPacketReceived?.Invoke(packetData);
                    Disconnect(packetData.Reason);
                    break;
                case VoicePacketTypes.ClientAudio:
                    if (IsConnected)
                        OnClientAudioPacketReceived?.Invoke((ClientAudio)packet.PacketData);
                    break;
                case VoicePacketTypes.ServerAudio:
                    if (IsConnected)
                        OnServerAudioPacketReceived?.Invoke((ServerAudio)packet.PacketData);
                    break;
                case VoicePacketTypes.UpdatePosition:
                    if (IsConnected)
                        OnUpdatePositionPacketReceived?.Invoke((UpdatePosition)packet.PacketData);
                    break;
                case VoicePacketTypes.Null:
                    if (IsConnected)
                        OnNullPacketReceived?.Invoke((Null)packet.PacketData);
                    break;
            }
        }

        //Dispose Handlers
        ~VoiceSocket()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    UDPSocket.Close();
                    IsConnected = false;
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
