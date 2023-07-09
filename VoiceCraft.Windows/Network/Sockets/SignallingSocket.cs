using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using VoiceCraft.Windows.Network.Packets;

namespace VoiceCraft.Windows.Network.Sockets
{
    public class SignallingSocket
    {
        //Variables
        private readonly NetworkManager NM;
        private readonly UdpClient Socket;
        private DateTime LastPing;

        //Events
        public delegate void Connect(ushort Key, int VoicePort);
        public delegate void ParticipantJoined(ushort Key, VoiceCraftParticipant Participant);
        public delegate void ParticipantLeft(ushort Key);
        public delegate void Bind(string Name);

        public event Connect? OnConnect;
        public event ParticipantJoined? OnParticipantJoined;
        public event ParticipantLeft? OnParticipantLeft;
        public event Bind? OnBind;

        public SignallingSocket(NetworkManager NM)
        {
            this.NM = NM;
            Socket = new UdpClient();
            LastPing = DateTime.UtcNow;
        }

        //Public Events
        public void StartConnect()
        {
            Socket.Connect(NM.IP, NM.Port);
            StartListeningAsync();
            var packet = new SignallingPacket()
            {
                PacketIdentifier = NM.ClientSided ? SignallingPacketIdentifiers.LoginClientSided : SignallingPacketIdentifiers.LoginServerSided,
                PacketKey = NM.Key,
                PacketVersion = App.Version,
            };
            SendPacket(packet.GetPacketDataStream());

            LastPing = DateTime.UtcNow;
            StartHeartbeatAsync();
        }

        public void StartDisconnect()
        {
            if (Socket.Client.Connected)
                Socket.Close();

            Socket.Dispose();
        }

        public void SendPacket(byte[] PacketStream)
        {
            if (Socket != null && Socket.Client != null && Socket.Client.Connected)
            {
                Socket.Send(PacketStream, PacketStream.Length);
            }
        }

        //Private Methods
        private async void StartListeningAsync()
        {
            while (!NM.Disconnecting)
            {
                try
                {
                    var data = await Socket.ReceiveAsync();
                    var packet = new SignallingPacket(data.Buffer);
                    LastPing = DateTime.UtcNow;
                    HandlePacket(packet);
                }
                catch (ObjectDisposedException)
                {
                    break; //Break out if UDPSocket is disconnected and disposed.
                }
                catch
                {
                    //Ignore every other exception except when the client is disconnected then break out of the loop.
                    if (NM.Disconnecting)
                        break;
                }
            }
        }

        private void HandlePacket(SignallingPacket Packet)
        {
            switch (Packet.PacketIdentifier)
            {
                case SignallingPacketIdentifiers.Accept:
                    OnConnect?.Invoke(Packet.PacketKey, Packet.PacketVoicePort);
                    break;
                case SignallingPacketIdentifiers.Deny:
                    NM.StartDisconnect(Packet.PacketMetadata ?? "");
                    break;
                case SignallingPacketIdentifiers.Login:
                    var Participant = new VoiceCraftParticipant(Packet.PacketMetadata ?? "Unknown", NM.RecordFormat, NM.RecordLengthMS);
                    OnParticipantJoined?.Invoke(Packet.PacketKey, Participant);
                    break;
                case SignallingPacketIdentifiers.Logout:
                    if (Packet.PacketKey == NM.Key)
                        NM.StartDisconnect(Packet.PacketMetadata ?? "Server Requested Disconnect");
                    else
                        OnParticipantLeft?.Invoke(Packet.PacketKey);
                    break;
                case SignallingPacketIdentifiers.Error:
                    NM.StartDisconnect(Packet.PacketMetadata ?? "A server error occurred...");
                    break;
                case SignallingPacketIdentifiers.Ping:
                    LastPing = DateTime.UtcNow;
                    break;
                case SignallingPacketIdentifiers.Binded:
                    OnBind?.Invoke(Packet.PacketMetadata ?? "Unknown");
                    break;
            }
        }

        private async void StartHeartbeatAsync()
        {
            while (!NM.Disconnecting)
            {
                try
                {
                    await Task.Delay(2000);

                    var packet = new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Ping, PacketVersion = App.Version }.GetPacketDataStream();
                    if (NM.Disconnecting)
                        return;

                    Socket.Send(packet, packet.Length);

                    if (DateTime.UtcNow.Subtract(LastPing).Seconds > 6)
                        NM.StartDisconnect("Connection timed out!");
                }
                catch (ObjectDisposedException)
                {
                    //Do nothing
                }
            }
        }
    }
}
