using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System;
using VCSignalling_Packet;
using System.Linq;

namespace VoiceCraft_Server.Servers
{
    public class Signalling
    {
        private Socket serverSocket;
        private EndPoint endPoint;

        //Events Here TODO
        public delegate void Fail(string reason);

        public event Fail OnFail;

        public Signalling()
        {
            try
            {
                Logger.LogToConsole(LogType.Info, $"Starting Signalling Server on port {ServerProperties._serverProperties.SignallingPort_UDP}", nameof(Signalling));

                endPoint = new IPEndPoint(IPAddress.Any, 0);

                //Bind Server to Address and Port
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, ServerProperties._serverProperties.SignallingPort_UDP);
                serverSocket.Bind(serverEp);
            }
            catch (Exception ex)
            {
                OnFail?.Invoke(ex.Message);
            }
        }

        public async Task StartServer()
        {
            Logger.LogToConsole(LogType.Success, "Signalling server successfully initialised.", nameof(Signalling));
            while (true)
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                SocketReceiveFromResult result = await serverSocket.ReceiveFromAsync(buffer, SocketFlags.None, endPoint);
                var packet = new SignallingPacket(buffer.Array);
                HandlePacket(packet, result.RemoteEndPoint);
            }
        }

        private async void HandlePacket(SignallingPacket _packet, EndPoint _endPoint)
        {
            switch(_packet.PacketDataIdentifier)
            {
                case PacketIdentifier.Login:
                    var KeyResult = _packet.PacketLoginId != null? _packet.PacketLoginId: "No Key Set";
                    Logger.LogToConsole(LogType.Info, $"Received Incoming Login: {KeyResult}", nameof(Signalling));
                    if (_packet.PacketVersion != MainEntry.Version)
                        await serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Deny }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                    else
                    {
                        string GeneratedKey = GenerateKey();
                        if (string.IsNullOrWhiteSpace(_packet.PacketLoginId))
                        {
                            while (true)
                            {
                                //Make sure the new generated key does not conflict with existing keys
                                if (ServerMetadata.voiceParticipants.Exists(x => x.LoginId == GeneratedKey))
                                    GeneratedKey = GenerateKey();
                                else
                                    break;
                            }
                            ServerMetadata.voiceParticipants.Add(new Participant() { LoginId = GeneratedKey, SignallingAddress = _endPoint });
                            await serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Accept, PacketLoginId = GeneratedKey, PacketVoicePort = ServerProperties._serverProperties.VoicePort_UDP }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                            Logger.LogToConsole(LogType.Success, $"Accepted Login: Key: {GeneratedKey}, Version: {_packet.PacketVersion}", nameof(Signalling));
                        }
                        else
                        {
                            if (ServerMetadata.voiceParticipants.Exists(x => x.LoginId == _packet.PacketLoginId && x.SignallingAddress.ToString() == _endPoint.ToString()))
                            {
                                await serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Deny }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                                Logger.LogToConsole(LogType.Warn, $"Denied Login: Key: {_packet.PacketLoginId}, Version: {_packet.PacketVersion} - Conflict Detected!", nameof(Signalling));
                                return;
                            }
                            //Make sure the key sent does not conflict with another key.
                            if (ServerMetadata.voiceParticipants.Exists(x => x.LoginId == _packet.PacketLoginId))
                            {
                                while (true)
                                {
                                    if (ServerMetadata.voiceParticipants.Exists(x => x.LoginId == GeneratedKey))
                                        GeneratedKey = GenerateKey();
                                    else
                                        break;
                                }
                                ServerMetadata.voiceParticipants.Add(new Participant() { LoginId = GeneratedKey, SignallingAddress = _endPoint });
                                await serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Accept, PacketLoginId = GeneratedKey, PacketVoicePort = ServerProperties._serverProperties.VoicePort_UDP }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                                Logger.LogToConsole(LogType.Success, $"Accepted Login: Key: {GeneratedKey}, Version: {_packet.PacketVersion}", nameof(Signalling));
                            }
                            else
                            {
                                ServerMetadata.voiceParticipants.Add(new Participant() { LoginId = _packet.PacketLoginId, SignallingAddress = _endPoint });
                                await serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Accept, PacketLoginId = _packet.PacketLoginId, PacketVoicePort = ServerProperties._serverProperties.VoicePort_UDP }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                                Logger.LogToConsole(LogType.Success, $"Accepted Login: Key: {_packet.PacketLoginId}, Version: {_packet.PacketVersion}", nameof(Signalling));
                            }
                            

                            var list = ServerMetadata.voiceParticipants.Where(x => x.LoginId != _packet.PacketLoginId).ToList();
                            for(int i = 0; i < list.Count; i++)
                            {
                                await serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Login, PacketLoginId = _packet.PacketLoginId, PacketName = _packet.PacketName }.GetPacketDataStream()), SocketFlags.None, list[i].SignallingAddress);
                                await serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Login, PacketLoginId = list[i].LoginId, PacketName = list[i].Name }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                            }
                        }
                    }
                    break;

                case PacketIdentifier.Ping:
                    var participant = ServerMetadata.voiceParticipants.FirstOrDefault(x => x.SignallingAddress.ToString() == _endPoint.ToString());
                    if(participant != null) {
                        participant.LastPing = DateTime.UtcNow;
                        await serverSocket.SendToAsync(new ArraySegment<byte>(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Ping }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                    }
                    break;
            }
        }

        private string GenerateKey()
        {
            Random res = new Random();
            string str = "abcdefghijklmnopqrstuvwxyz0123456789";
            int size = 5;

            string RandomString = "";

            for (int i = 0; i < size; i++)
            {
                int x = res.Next(str.Length);
                RandomString += str[x];
            }

            return RandomString;
        }
    }
}
