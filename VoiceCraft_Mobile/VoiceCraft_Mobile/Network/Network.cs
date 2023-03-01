using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using VCSignalling_Packet;
using VCVoice_Packet;
using VoiceCraft_Mobile.ViewModels;

namespace VoiceCraft_Mobile.Network
{
    public class Network
    {
        public UdpClient signallingClient { get; private set; }
        public UdpClient voiceClient { get; private set; }
        public string serverIp { get; set; } = "";
        public string loginId { get; set; } = "";
        public string localServerId { get; set; } = "";
        public Network()
        {
            signallingClient = new UdpClient();
            voiceClient = new UdpClient();
        }

        public async Task<bool> ConnectAndLoginAsync(string IP, int PORT, string LOGINID)
        {
            try
            {
                serverIp = IP;
                signallingClient.Connect(IP, PORT);
                var packet = new SignallingPacket() { PacketDataIdentifier = VCSignalling_Packet.PacketIdentifier.Login, PacketLoginId = LOGINID, PacketVersion = "v1.3.0-alpha" }.GetPacketDataStream();
                await signallingClient.SendAsync(packet, packet.Length);

                var operation = signallingClient.ReceiveAsync();
                var result = await Task.WhenAny(operation, Task.Delay(5000));
                if(result != operation)
                {
                    Disconnect();
                    throw new Exception("Could not reach server: Timed Out");
                }
                var dataReceived = await operation;
                var packetReceived = new SignallingPacket(dataReceived.Buffer);

                if (packetReceived.PacketDataIdentifier == VCSignalling_Packet.PacketIdentifier.Deny)
                {
                    Disconnect();
                    throw new Exception("Server denied login");
                }
                loginId = packetReceived.PacketLoginId;
                return true;
            }
            catch (Exception ex)
            {
                Disconnect();
                await Utils.DisplayAlertAsync("Error", ex.Message);
                return false; 
            }
        }

        public async Task<bool> ConnectToVoiceAsync(string IP, int PORT)
        {
            try
            {
                voiceClient.Connect(IP, PORT);
                var packet = new VoicePacket() { PacketDataIdentifier = VCVoice_Packet.PacketIdentifier.Login, PacketLoginId = loginId, PacketVersion = "v1.3.0-alpha" }.GetPacketDataStream();
                await voiceClient.SendAsync(packet, packet.Length);

                var operation = voiceClient.ReceiveAsync();
                var result = await Task.WhenAny(operation, Task.Delay(5000));
                if (result != operation)
                {
                    Disconnect();
                    throw new Exception("Could not reach server: Timed Out");
                }
                var dataReceived = await operation;
                var packetReceived = new VoicePacket(dataReceived.Buffer);

                if (packetReceived.PacketDataIdentifier == VCVoice_Packet.PacketIdentifier.Deny)
                {
                    Disconnect();
                    throw new Exception("Server denied login");
                }
                return true;
            }
            catch (Exception ex)
            {
                Disconnect();
                await Utils.DisplayAlertAsync("Error", $"Could not connect Voice:{ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            loginId = "";
            if (signallingClient.Client != null && signallingClient.Client.Connected)
            {
                signallingClient.Close();
                voiceClient.Dispose();
                signallingClient = new UdpClient();
            }
            if(voiceClient.Client != null && voiceClient.Client.Connected)
            {
                voiceClient.Close();
                voiceClient.Dispose();
                voiceClient = new UdpClient();
            }
                
        }

        public static Network Current { get; private set; } = new Network();
    }

    public class SignallingNetworkHandler
    {
        public static async Task Listen()
        {
            while(Network.Current.signallingClient.Client.Connected)
            {
                var result = await Network.Current.signallingClient.ReceiveAsync();
                var packet = new SignallingPacket(result.Buffer);
                HandlePacket(packet);
            }
        }

        private static async Task HandlePacket(SignallingPacket packet)
        {
            switch(packet.PacketDataIdentifier)
            {
                case VCSignalling_Packet.PacketIdentifier.Binded:
                    await Network.Current.ConnectToVoiceAsync(Network.Current.serverIp, packet.PacketVoicePort);
                    break;
                case VCSignalling_Packet.PacketIdentifier.Login:
                    BaseViewModel.participants.Add(new Models.ParticipantModel() { LoginId = packet.PacketLoginId, Name = packet.PacketName});
                    break;
            }
        }
    }

    public class VoiceNetworkHandler
    {
        public static async Task Listen()
        {
            while(Network.Current.voiceClient.Client.Connected)
            {
                var result = await Network.Current.voiceClient.ReceiveAsync();
                var packet = new VoicePacket(result.Buffer);
                HandlePacket(packet);
            }
        }

        private static async Task HandlePacket(VoicePacket packet)
        {
            switch(packet.PacketDataIdentifier)
            {
                //Do Stuff
            }
        }
    }
}
