using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using VoiceCraft.Mobile.Network.Interfaces;
using VoiceCraft.Mobile.Network.Packets;

namespace VoiceCraft.Mobile.Network.Sockets
{
    public class SignallingSocket : INetwork
    {
        public INetworkManager Manager { get; }
#nullable enable
        public UdpClient? UDPSocket { get; set; }
#nullable disable
        public SignallingSocket(INetworkManager Manager) => this.Manager = Manager;
        private bool StartDisconnect = false;
        private DateTime LastPing = DateTime.UtcNow;

        //Public Methods
        public void Connect()
        {
            try
            {
                UDPSocket = new UdpClient();
                UDPSocket.Connect(Manager.IP, Manager.Port);
                StartListeningAsync();
                var packet = new SignallingPacket() { 
                    PacketIdentifier = Manager.ClientSidedPositioning? SignallingPacketIdentifiers.LoginClientSided : SignallingPacketIdentifiers.LoginServerSided,
                    PacketKey = Manager.Key,
                    PacketVersion = App.Version,
                    PacketCodec = Manager.Codec
                };
                SendPacket(packet.GetPacketDataStream());

                LastPing = DateTime.UtcNow;
                StartHeartbeatAsync();
            }
            catch(Exception ex)
            {
                Manager.PerformConnectError(SocketTypes.Signalling, ex.Message);
            }
        }

        public void Disconnect()
        {
            if (UDPSocket != null)
            {
                StartDisconnect = true;
                if (UDPSocket.Client.Connected)
                    UDPSocket.Close();

                UDPSocket.Dispose();
                UDPSocket = null;
            }
        }

        public void SendPacket(byte[] PacketStream)
        {
            if(UDPSocket != null && UDPSocket.Client.Connected)
            {
                UDPSocket.Send(PacketStream, PacketStream.Length);
            }
        }

        //Private Methods
        private async void StartListeningAsync()
        {
            while (!StartDisconnect)
            {
                try
                {
                    var data = await UDPSocket.ReceiveAsync();
                    var packet = new SignallingPacket(data.Buffer);
                    LastPing = DateTime.UtcNow;
                    HandlePacket(packet);
                }
                catch (ObjectDisposedException)
                {
                    break; //Break out if UDPSocket is disconnected and disposed.
                }
                catch {
                    //Ignore every other exception except when the client is disconnected then break out of the loop.
                    if (StartDisconnect)
                        break;
                }
            }
        }

        private void HandlePacket(SignallingPacket Packet)
        {
            switch(Packet.PacketIdentifier)
            {
                case SignallingPacketIdentifiers.Accept16:
                    Manager.VoicePort = Packet.PacketVoicePort;
                    Manager.PerformConnect(SocketTypes.Signalling, 16000, Packet.PacketKey);
                    break;
                case SignallingPacketIdentifiers.Accept48:
                    Manager.VoicePort = Packet.PacketVoicePort;
                    Manager.PerformConnect(SocketTypes.Signalling, 48000, Packet.PacketKey);
                    break;
                case SignallingPacketIdentifiers.Deny:
                    Manager.PerformConnectError(SocketTypes.Signalling, Packet.PacketMetadata);
                    break;
                case SignallingPacketIdentifiers.Login:
                    var Participant = new VoiceCraftParticipant(Packet.PacketMetadata, Manager.RecordFormat, Manager.AudioFrameSizeMS, Packet.PacketCodec);
                    Manager.PerformParticipantJoined(Packet.PacketKey, Participant);
                    break;
                case SignallingPacketIdentifiers.Logout:
                    if (Packet.PacketKey == Manager.Key)
                        Manager.Disconnect(Packet.PacketMetadata);
                    else
                        Manager.PerformParticipantLeft(Packet.PacketKey);
                    break;
                case SignallingPacketIdentifiers.Error:
                    Manager.Disconnect(Packet.PacketMetadata);
                    break;
                case SignallingPacketIdentifiers.Ping:
                    LastPing = DateTime.UtcNow;
                    break;
                case SignallingPacketIdentifiers.Binded:
                    Manager.PerformBinded(Packet.PacketMetadata);
                    break;
            }
        }

        private async void StartHeartbeatAsync()
        {
            while (!StartDisconnect)
            {
                try
                {
                    await Task.Delay(2000);

                    var packet = new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Ping, PacketVersion = App.Version }.GetPacketDataStream();
                    UDPSocket.Send(packet, packet.Length);

                    if (DateTime.UtcNow.Subtract(LastPing).Seconds > 10)
                        Manager.Disconnect("Connection timed out!");
                }
                catch
                {
                }
            }
        }
    }
}
