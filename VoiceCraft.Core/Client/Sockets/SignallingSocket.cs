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
    public class SignallingSocket : IDisposable
    {
        public Socket TCPSocket { get; }
        public CancellationToken CTS { get; }
        public bool Connected { get; private set; }
        public bool IsDisposed { get; private set; }

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

        public delegate void SocketConnected(ushort LoginKey, ushort VoicePort);
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
        public event SocketConnected? OnSocketConnected;


        public SignallingSocket(CancellationToken Token)
        {
            TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CTS = Token;
        }

        public async void ConnectAsync(string IP, int Port, ushort LoginKey = 0, PositioningTypes PositioningType = PositioningTypes.ServerSided, string Version = "")
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(SignallingSocket));

            try
            {
                var cancelTask = Task.Delay(2000);
                var connectTask = TCPSocket.ConnectAsync(IP, Port);
                await await Task.WhenAny(connectTask, cancelTask);
                if(cancelTask.IsCompleted) throw new Exception("TCP socket timed out.");

                ListenAsync();
                SendPacketAsync(new SignallingPacket()
                {
                    PacketType = SignallingPacketTypes.Login,
                    PacketData = new Login()
                    {
                        LoginKey = LoginKey,
                        PositioningType = PositioningType,
                        Version = Version
                    }
                });

                await Task.Delay(3000);
                if (!Connected) throw new Exception("Signalling timed out");
            }
            catch(Exception ex)
            {
                if(!CTS.IsCancellationRequested)
                    OnSocketDisconnected?.Invoke(ex.Message);
            }
        }

        public async void SendPacketAsync(ISignallingPacket packet)
        {
            if (TCPSocket.Connected)
            {
                var packetStream = packet.GetPacketStream();
                await TCPSocket.SendAsync(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                await TCPSocket.SendAsync(packetStream, SocketFlags.None);
            }
        }

        public void SendPacket(ISignallingPacket packet)
        {
            if (TCPSocket.Connected)
            {
                var packetStream = packet.GetPacketStream();
                TCPSocket.Send(BitConverter.GetBytes((ushort)(packetStream.Length)), SocketFlags.None);
                TCPSocket.Send(packetStream, SocketFlags.None);
            }
        }

        public void Disconnect(string? reason = null)
        {
            try
            {
                if (TCPSocket.Connected)
                {
                    TCPSocket.Disconnect(true);
                }
                if(reason != null && !CTS.IsCancellationRequested) OnSocketDisconnected?.Invoke(reason);
                Connected = false;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine(ex);
#endif
            }
        }

        public void Dispose()
        {
            TCPSocket.Dispose();
            IsDisposed = true;
        }

        private async void ListenAsync()
        {
            byte[]? packetBuffer = null;
            byte[] lengthBuffer = new byte[2];
            var stream = new NetworkStream(TCPSocket);
            while (TCPSocket.Connected && !CTS.IsCancellationRequested)
            {
                try
                {
                    //TCP Is Annoying
                    var bytes = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length).ConfigureAwait(false);
                    if (bytes == 0) break; //Socket is closed.

                    ushort packetLength = SignallingPacket.GetPacketLength(lengthBuffer);
                    //If packets are an invalid length then we break out to prevent memory exceptions
                    if(packetLength > 1024)
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

            await stream.DisposeAsync();
        }

        private void HandlePacket(SignallingPacket packet)
        {
            switch (packet.PacketType)
            {
                case SignallingPacketTypes.Login:
                    if(Connected)
                        OnLoginPacketReceived?.Invoke((Login)packet.PacketData);
                    break;
                case SignallingPacketTypes.Logout:
                    if (Connected)
                        OnLogoutPacketReceived?.Invoke((Logout)packet.PacketData);
                    break;
                case SignallingPacketTypes.Accept:
                    Connected = true;
                    OnAcceptPacketReceived?.Invoke((Accept)packet.PacketData);
                    break;
                case SignallingPacketTypes.Deny:
                    OnDenyPacketReceived?.Invoke((Deny)packet.PacketData);
                    break;
                case SignallingPacketTypes.Binded:
                    if (Connected)
                        OnBindedPacketReceived?.Invoke((Binded)packet.PacketData);
                    break;
                case SignallingPacketTypes.Unbinded:
                    if (Connected)
                        OnUnbindedPacketReceived?.Invoke((Unbinded)packet.PacketData);
                    break;
                case SignallingPacketTypes.Deafen:
                    if (Connected)
                        OnDeafenPacketReceived?.Invoke((Deafen)packet.PacketData);
                    break;
                case SignallingPacketTypes.Undeafen:
                    if (Connected)
                        OnUndeafenPacketReceived?.Invoke((Undeafen)packet.PacketData);
                    break;
                case SignallingPacketTypes.Mute:
                    if (Connected)
                        OnMutePacketReceived?.Invoke((Mute)packet.PacketData);
                    break;
                case SignallingPacketTypes.Unmute:
                    if (Connected)
                        OnUnmutePacketReceived?.Invoke((Unmute)packet.PacketData);
                    break;
                case SignallingPacketTypes.Error:
                    if (Connected)
                        OnErrorPacketReceived?.Invoke((Error)packet.PacketData);
                    break;
                case SignallingPacketTypes.Ping:
                    if (Connected)
                        OnPingPacketReceived?.Invoke((Ping)packet.PacketData);
                    break;
                case SignallingPacketTypes.Null:
                    if (Connected)
                        OnNullPacketReceived?.Invoke((Null)packet.PacketData);
                    break;
            }
        }
    }
}
