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
        public CancellationTokenSource CTS { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsDisposed { get; private set; }
        public DateTime LastActive { get; private set; }

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


        public SignallingSocket()
        {
            TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CTS = new CancellationTokenSource();
        }

        public async Task ConnectAsync(string IP, int Port, ushort LoginKey = 0, PositioningTypes PositioningType = PositioningTypes.ServerSided, string Version = "")
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(SignallingSocket));
            if (IsConnected) throw new InvalidOperationException("You must disconnect before connecting!");

            try
            {
                var cancelTask = Task.Delay(2000);
                var connectTask = TCPSocket.ConnectAsync(IP, Port);
                await await Task.WhenAny(connectTask, cancelTask);
                if(cancelTask.IsCompleted) throw new Exception("TCP socket timed out.");

                _ = ListenAsync();
                await SendPacketAsync(Login.Create(PositioningType, LoginKey, false, false, string.Empty, Version));

                await Task.Delay(5000);
                if (!IsConnected) throw new Exception("Signalling timed out");
            }
            catch(Exception ex)
            {
                Disconnect(ex.Message);
            }
        }

        public async Task SendPacketAsync(ISignallingPacket packet)
        {
            if (TCPSocket.Connected)
            {
                try
                {
                    var packetStream = packet.GetPacketStream();
                    await TCPSocket.SendAsync(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                    await TCPSocket.SendAsync(packetStream, SocketFlags.None);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex);
#endif
                }
            }
        }

        public void SendPacket(ISignallingPacket packet)
        {
            if (TCPSocket.Connected)
            {
                try
                {
                    var packetStream = packet.GetPacketStream();
                    TCPSocket.Send(BitConverter.GetBytes((ushort)packetStream.Length), SocketFlags.None);
                    TCPSocket.Send(packetStream, SocketFlags.None);
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debug.WriteLine(ex);
#endif
                }
            }
        }

        public void Disconnect(string? reason = null)
        {
            try
            {
                CTS.Cancel();
                if (TCPSocket.Connected)
                {
                    TCPSocket.Disconnect(true);
                }
                if(!string.IsNullOrWhiteSpace(reason)) OnSocketDisconnected?.Invoke(reason);
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
            byte[]? packetBuffer = null;
            byte[] lengthBuffer = new byte[2];
            var stream = new NetworkStream(TCPSocket);
            while (TCPSocket.Connected && !CTS.IsCancellationRequested)
            {
                try
                {
                    //TCP Is Annoying
                    var bytes = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length).ConfigureAwait(false);
                    if (bytes == 0)
                    {
                        if (!TCPSocket.Connected || CTS.IsCancellationRequested)
                            break;
                    }//Socket is closed.

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
            LastActive = DateTime.UtcNow;
            switch (packet.PacketType)
            {
                case SignallingPacketTypes.Login:
                    if(IsConnected)
                        OnLoginPacketReceived?.Invoke((Login)packet.PacketData);
                    break;
                case SignallingPacketTypes.Logout:
                    if (IsConnected)
                        OnLogoutPacketReceived?.Invoke((Logout)packet.PacketData);
                    break;
                case SignallingPacketTypes.Accept:
                    IsConnected = true;
                    OnAcceptPacketReceived?.Invoke((Accept)packet.PacketData);
                    break;
                case SignallingPacketTypes.Deny:
                    var packetData = (Deny)packet.PacketData;
                    OnDenyPacketReceived?.Invoke(packetData);
                    Disconnect(packetData.Reason);
                    break;
                case SignallingPacketTypes.Binded:
                    if (IsConnected)
                        OnBindedPacketReceived?.Invoke((Binded)packet.PacketData);
                    break;
                case SignallingPacketTypes.Unbinded:
                    if (IsConnected)
                        OnUnbindedPacketReceived?.Invoke((Unbinded)packet.PacketData);
                    break;
                case SignallingPacketTypes.Deafen:
                    if (IsConnected)
                        OnDeafenPacketReceived?.Invoke((Deafen)packet.PacketData);
                    break;
                case SignallingPacketTypes.Undeafen:
                    if (IsConnected)
                        OnUndeafenPacketReceived?.Invoke((Undeafen)packet.PacketData);
                    break;
                case SignallingPacketTypes.Mute:
                    if (IsConnected)
                        OnMutePacketReceived?.Invoke((Mute)packet.PacketData);
                    break;
                case SignallingPacketTypes.Unmute:
                    if (IsConnected)
                        OnUnmutePacketReceived?.Invoke((Unmute)packet.PacketData);
                    break;
                case SignallingPacketTypes.Error:
                    if (IsConnected)
                        OnErrorPacketReceived?.Invoke((Error)packet.PacketData);
                    break;
                case SignallingPacketTypes.Ping:
                    if (IsConnected)
                        OnPingPacketReceived?.Invoke((Ping)packet.PacketData);
                    break;
                case SignallingPacketTypes.Null:
                    if (IsConnected)
                        OnNullPacketReceived?.Invoke((Null)packet.PacketData);
                    break;
            }
        }

        //Dispose Handlers
        ~SignallingSocket()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    TCPSocket.Close();
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
