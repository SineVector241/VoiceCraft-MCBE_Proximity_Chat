using System;
using System.Net.Sockets;
using VoiceCraft.Mobile.Network.Interfaces;
using VoiceCraft.Mobile.Network.Packets;

namespace VoiceCraft.Mobile.Network.Sockets
{
    public class VoiceSocket : INetwork
    {
        public INetworkManager Manager { get; }
        public UdpClient UDPSocket { get; set; }
        public VoiceSocket(INetworkManager Manager) => this.Manager = Manager;
        private bool StartDisconnect = false;

        public void Connect()
        {
            try
            {
                UDPSocket = new UdpClient();
                UDPSocket.Connect(Manager.IP, Manager.VoicePort);
                StartListeningAsync();
                var packet = new VoicePacket()
                {
                    PacketIdentifier = VoicePacketIdentifier.Login,
                    PacketKey = Manager.Key
                };
                SendPacket(packet.GetPacketDataStream());
            }
            catch (Exception ex)
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
            if (UDPSocket != null && UDPSocket.Client.Connected)
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
                    var packet = new VoicePacket(data.Buffer);
                    HandlePacketAsync(packet);
                }
                catch (ObjectDisposedException)
                {
                    break; //Break out if UDPSocket is disconnected and disposed.
                }
                catch
                {
                    //Ignore every other exception except when the client is disconnected then break out of the loop.
                    if (StartDisconnect)
                        break;
                }
            }
        }

        private void HandlePacketAsync(VoicePacket Packet)
        {
            switch (Packet.PacketIdentifier)
            {
                case VoicePacketIdentifier.Accept:
                    Manager.PerformConnect(SocketTypes.Voice);
                    break;
                case VoicePacketIdentifier.Deny:
                    Manager.PerformConnectError(SocketTypes.Voice, "Server denied voice connection.");
                    break;
                case VoicePacketIdentifier.Audio:
                    VoiceCraftParticipant participant = null;
                    Manager.Participants.TryGetValue(Packet.PacketKey, out participant);

                    if (participant != null)
                    {
                        var rotationSource = Math.Atan2(Packet.PacketPosition.X, Packet.PacketPosition.Z);
                        if(!Manager.ClientSidedPositioning && Manager.DirectionalHearing)
                        {
                            participant.AudioProvider.RightVolume = (float)(0.5 + Math.Sin(rotationSource) * 0.5);
                            participant.AudioProvider.LeftVolume = (float)(0.5 - Math.Sin(rotationSource) * 0.5);
                        }

                        participant.AddAudioSamples(Packet.PacketAudio, Packet.PacketCount);
                    }
                    break;
                case VoicePacketIdentifier.Error:
                    Manager.Disconnect("A server error occured with voice...");
                    break;
            }
        }
    }
}
