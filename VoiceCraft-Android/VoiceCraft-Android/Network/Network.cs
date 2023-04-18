using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using VCSignalling_Packet;
using VCVoice_Packet;
using VoiceCraft_Android.Models;
using VoiceCraft_Android.Services;
using Xamarin.Forms;

namespace VoiceCraft_Android.Network
{
    public class Network
    {
        public const string Version = "v1.3.0-alpha";
    }

    public class SignallingClient
    {
        private UdpClient Client = new UdpClient();
        private bool isConnecting = false;
        private bool isConnected = false;
        private string serverName = "";

        public string Key { get; private set; } = string.Empty;
        public string hostName { get; private set; } = string.Empty;
        public int VoicePort { get; private set; } = 0;

        //Heartbeat Variables
        private DateTime lastPing;
        private bool stopTimer = false;

        //Events
        public delegate Task Connected(string key, string serverName);
        public delegate Task Disconnected(string reason);
        public delegate Task Binded(string name);
        public delegate Task AddParticipant(ParticipantModel participant);
        public delegate Task RemoveParticipant(string key);

        public event Connected OnConnect;
        public event Disconnected OnDisconnect;
        public event Binded OnBinded;
        public event AddParticipant OnParticipantLogin;
        public event RemoveParticipant OnParticipantLogout;

        public void Connect(string IP, int PORT, string Key = null, string servName = null)
        {
            try
            {
                serverName = servName;
                hostName = IP;
                isConnecting = true;

                Client.Connect(IP, PORT);
                StartListeningAsync();
                lastPing = DateTime.UtcNow;
                stopTimer = false;

                var packet = new SignallingPacket() { PacketDataIdentifier = VCSignalling_Packet.PacketIdentifier.Login, PacketVersion = Network.Version, PacketLoginId = Key }.GetPacketDataStream();
                Client.Send(packet, packet.Length);

                Device.StartTimer(TimeSpan.FromSeconds(2), SendHeartbeatAsync);
            }
            catch(Exception ex)
            {
                OnDisconnect?.Invoke(ex.Message);
            }
        }

        public void Disconnect(string reason = null)
        {
            OnDisconnect?.Invoke(reason);

            Client.Close();
            Client.Dispose();
            Client = new UdpClient();

            Key = string.Empty;
            VoicePort = 0;
            isConnected = false;
            isConnecting = false;
            stopTimer = true;
        }

        public void Send(SignallingPacket packet)
        {
            if (!isConnected)
                return;

            var stream = packet.GetPacketDataStream();
            Client.Send(stream, stream.Length);
        }

        private async Task StartListeningAsync()
        {

            while (true)
            {
                try
                {
                    var data = await Client.ReceiveAsync();
                    var packet = new SignallingPacket(data.Buffer);
                    lastPing = DateTime.UtcNow;

                    //Handle Packet Here
                    switch (packet.PacketDataIdentifier)
                    {
                        case VCSignalling_Packet.PacketIdentifier.Accept:
                            Key = packet.PacketLoginId;
                            VoicePort = packet.PacketVoicePort;
                            isConnected = true;
                            isConnecting = false;
                            OnConnect?.Invoke(Key, serverName);
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Ping:
                            lastPing = DateTime.UtcNow;
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Deny:
                            Disconnect("Server Denied Login Request. Possible LoginId Conflict");
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Binded:
                            OnBinded?.Invoke(packet.PacketName);
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Login:
                            OnParticipantLogin?.Invoke(new ParticipantModel
                            {
                                LoginKey = packet.PacketLoginId,
                                Name = packet.PacketName,
                                WaveProvider = new NAudio.Wave.BufferedWaveProvider(VoipService.GetRecordFormat)
                            });
                            break;

                        case VCSignalling_Packet.PacketIdentifier.Logout:
                            OnParticipantLogout?.Invoke(packet.PacketLoginId);
                            break;
                    }
                }
                catch (ObjectDisposedException)
                { //Ignore this exception. Usually means when the client is disposed this will throw.
                    break; //Should break out actually.
                }
                catch { }
                //Handle everything else but do nothing for the moment.
            }
        }

        private bool SendHeartbeatAsync()
        {
            try
            {
                if (!isConnected && !isConnecting)
                    return false;

                var packet = new SignallingPacket() { PacketDataIdentifier = VCSignalling_Packet.PacketIdentifier.Ping }.GetPacketDataStream();
                Client.Send(packet, packet.Length);

                if (DateTime.UtcNow.Subtract(lastPing).Seconds > 10)
                {
                    Disconnect("Connection Timed Out.");
                    stopTimer = true;
                }

                return !stopTimer;
            }
            catch
            {
                return !stopTimer;
            }
        }
    }


    public class VoiceClient
    {
        public UdpClient Client { get; private set; } = new UdpClient();

        private bool isConnected = false;
        private DateTime startedConnection = DateTime.UtcNow;

        //Events
        public delegate Task Connected();
        public delegate Task Disconnected(string reason);
        public delegate Task AudioReceived(byte[] Audio, string Key);

        public event Connected OnConnect;
        public event Disconnected OnDisconnect;
        public event AudioReceived OnAudioReceived;

        public void Connect(string IP, int PORT, string Key = null)
        {
            try
            {
                isConnected = false;
                startedConnection = DateTime.UtcNow;

                Client.Connect(IP, PORT);
                StartListeningAsync();

                var packet = new VoicePacket() { PacketDataIdentifier = VCVoice_Packet.PacketIdentifier.Login, PacketVersion = Network.Version, PacketLoginId = Key }.GetPacketDataStream();
                Client.Send(packet, packet.Length);

                Device.StartTimer(TimeSpan.FromSeconds(1), WaitForConnection);
            }
            catch(Exception ex)
            {
                OnDisconnect?.Invoke(ex.Message);
            }
        }

        public void Disconnect(string reason = null)
        {
            isConnected = false;

            OnDisconnect?.Invoke(reason);

            Client.Close();
            Client.Dispose();
            Client = new UdpClient();
        }

        public void Send(VoicePacket packet)
        {
            var stream = packet.GetPacketDataStream();
            Client.Send(stream, stream.Length);
        }

        private async Task StartListeningAsync()
        {
            while (true)
            {
                try
                {
                    var data = await Client.ReceiveAsync();
                    var packet = new VoicePacket(data.Buffer);

                    //Handle Packet Here
                    switch (packet.PacketDataIdentifier)
                    {
                        case VCVoice_Packet.PacketIdentifier.Accept:
                            OnConnect?.Invoke();
                            isConnected = true;
                            break;

                        case VCVoice_Packet.PacketIdentifier.Deny:
                            Disconnect("Voice Server Denied Login Request.");
                            break;

                        case VCVoice_Packet.PacketIdentifier.Audio:
                            OnAudioReceived?.Invoke(packet.PacketAudio, packet.PacketLoginId);
                            break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private bool WaitForConnection()
        {
            if (isConnected)
            {
                return false;
            }
            if (DateTime.UtcNow.Subtract(startedConnection).Seconds > 5)
            {
                Disconnect("Voice Connection Timed Out.");
                return false;
            }
            return true;
        }
    }
}
