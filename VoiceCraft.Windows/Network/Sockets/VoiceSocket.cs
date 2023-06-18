using System.Net.Sockets;
using System;
using System.Threading.Tasks;
using VoiceCraft.Windows.Network.Packets;
using System.Numerics;

namespace VoiceCraft.Windows.Network.Sockets
{
    public class VoiceSocket
    {
        //Variables
        private readonly NetworkManager NM;
        private readonly UdpClient Socket;
        private bool IsConnected;

        //Events
        public delegate void Connect();

        public event Connect? OnConnect;

        public VoiceSocket(NetworkManager NM)
        {
            this.NM = NM;
            Socket = new UdpClient();
        }

        public void StartConnect()
        {
            Socket.Connect(NM.IP, NM.VoicePort);
            StartListeningAsync();
            var packet = new VoicePacket()
            {
                PacketIdentifier = VoicePacketIdentifier.Login,
                PacketKey = NM.Key
            };
            SendPacket(packet.GetPacketDataStream());

            WaitForConnectionAsync();
        }

        public void StartDisconnect()
        {
            if (Socket.Client.Connected)
                Socket.Close();

            Socket.Dispose();
        }

        public void SendPacket(byte[] PacketStream)
        {
            if (Socket.Client.Connected)
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
                    var packet = new VoicePacket(data.Buffer);
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

        private void HandlePacket(VoicePacket Packet)
        {
            switch (Packet.PacketIdentifier)
            {
                case VoicePacketIdentifier.Accept:
                    IsConnected = true;
                    OnConnect?.Invoke();
                    break;
                case VoicePacketIdentifier.Deny:
                    NM.StartDisconnect("Server denied voice connection.");
                    break;
                case VoicePacketIdentifier.Audio:
                    _ = Task.Run(() => {
                        NM.Participants.TryGetValue(Packet.PacketKey, out VoiceCraftParticipant? participant);

                        if (participant != null && Packet.PacketAudio != null)
                        {
                            var volume = 1 - Vector3.Distance(Packet.PacketPosition, new Vector3()) / Packet.PacketDistance;
                            participant.SetVolume(volume);
                            var rotationSource = Math.Atan2(Packet.PacketPosition.X, Packet.PacketPosition.Z);
                            if (!NM.ClientSided && NM.DirectionalHearing)
                            {
                                participant.AudioProvider.RightVolume = (float)(0.5 + Math.Sin(rotationSource) * 0.5);
                                participant.AudioProvider.LeftVolume = (float)(0.5 - Math.Sin(rotationSource) * 0.5);
                            }
                            participant.AddAudioSamples(Packet.PacketAudio, Packet.PacketCount);
                        }
                    });
                    break;
                case VoicePacketIdentifier.Error:
                    NM.StartDisconnect("A server error occured with voice...");
                    break;
            }
        }

        private async void WaitForConnectionAsync()
        {
            var startedConnection = DateTime.UtcNow;
            while (true)
            {
                await Task.Delay(1000);
                if (IsConnected)
                    break;

                if (DateTime.UtcNow.Subtract(startedConnection).Seconds > 5)
                {
                    NM.StartDisconnect("Voice connection timed out.");
                    break;
                }
            }
        }
    }
}
