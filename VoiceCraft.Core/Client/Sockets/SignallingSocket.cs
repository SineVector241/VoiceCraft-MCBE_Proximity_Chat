using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.Interfaces;
using VoiceCraft.Core.Packets.Signalling;

namespace VoiceCraft.Core.Client.Sockets
{
    public class SignallingSocket
    {
        public Socket TCPSocket { get; }
        public CancellationToken CTS { get; }

        //Delegates
        public delegate void LoginPacket(Login packet);
        public delegate void LogoutPacket(Logout packet);
        public delegate void AcceptPacket(Accept packet);
        public delegate void DenyPacket(Deny packet);
        public delegate void BindedPacket(Binded packet);
        public delegate void UnbindedPacket(Unbinded packet);
        public delegate void DeafenPacket(Deafen packet);
        public delegate void UndeafenPacket(Undeafen packet);
        public delegate void MutePacket(Mute packet);
        public delegate void UnmutePacket(Unmute packet);
        public delegate void ErrorPacket(Error packet);
        public delegate void PingPacket(Ping packet);
        public delegate void NullPacket(Null packet);

        public delegate void SocketDisconnected(string reason);

        //Events
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

        public async Task ConnectAsync(string IP, int Port)
        {
            try
            {
                await TCPSocket.ConnectAsync(IP, Port);
                ListenAsync();
            }
            catch(Exception ex)
            {
                if(!CTS.IsCancellationRequested)
                    OnSocketDisconnected?.Invoke(ex.Message);
            }
        }

        public async void SendPacketAsync(ISignallingPacket packet)
        {
            if(TCPSocket.Connected)
                await TCPSocket.SendAsync(packet.GetPacketStream(), SocketFlags.None);
        }

        public void SendPacket(ISignallingPacket packet)
        {
            if (TCPSocket.Connected)
                TCPSocket.Send(packet.GetPacketStream(), SocketFlags.None);
        }

        public void Disconnect(string? reason = null)
        {
            try
            {
                if (TCPSocket.Connected)
                {
                    TCPSocket.Close();
                }
                if(reason != null && !CTS.IsCancellationRequested) OnSocketDisconnected?.Invoke(reason);
                TCPSocket.Dispose();
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
            while (TCPSocket.Connected && !CTS.IsCancellationRequested)
            {
                try
                {
                    var buffer = new byte[1024];
                    var networkStream = await TCPSocket.ReceiveAsync(buffer, SocketFlags.None);
                    var packet = new SignallingPacket(buffer);
                    HandlePacket(packet);
                }
                catch (SocketException ex)
                {
                    if (!TCPSocket.Connected || CTS.IsCancellationRequested || ex.ErrorCode == 995) //Break out and dispose if its an IO exception or if TCP is not connected or disconnect requested.
                    {
                        Disconnect(ex.Message);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (!TCPSocket.Connected || CTS.IsCancellationRequested)
                    {
                        Disconnect(ex.Message);
                        break;
                    }
                }
            }
        }

        private void HandlePacket(SignallingPacket packet)
        {
            switch (packet.PacketType)
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
                case SignallingPacketTypes.Unbinded:
                    OnUnbindedPacketReceived?.Invoke((Unbinded)packet.PacketData);
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
                    OnUnmutePacketReceived?.Invoke((Unmute)packet.PacketData);
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
