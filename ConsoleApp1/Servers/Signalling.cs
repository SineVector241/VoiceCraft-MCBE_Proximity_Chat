using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using VoiceCraft_Server.Data;
using System;
using VCSignalling_Packet;
using System.Linq;

namespace VoiceCraft_Server.Servers
{
    public class Signalling
    {
        private ServerData serverData;
        private Socket socket;
        private EndPoint endPoint;

        public Signalling(ServerData serverDataObject)
        {
            try
            {
                serverData = serverDataObject;
                endPoint = new IPEndPoint(IPAddress.Any, 0);

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                ServerEvents.OnParticipantBinded += OnParticipantBinded;
                ServerEvents.OnParticipantLogout += OnParticipantLogout;
            }
            catch (Exception ex)
            {
                ServerEvents.InvokeFailed(nameof(Signalling), ex.Message);
            }
        }

        public async Task Start()
        {
            try
            {
                Logger.LogToConsole(LogType.Info, "Starting Server", nameof(Signalling));

                IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, ServerProperties._serverProperties.SignallingPort_UDP);
                socket.Bind(serverEp);

                ServerEvents.InvokeStarted(nameof(Signalling));

                while (true)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
                        SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, endPoint);
                        var packet = new SignallingPacket(buffer.Array);
                        HandlePacket(packet, result.RemoteEndPoint);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogToConsole(LogType.Error, ex.Message, nameof(Signalling));
                    }
                }
            }
            catch (Exception ex)
            {
                ServerEvents.InvokeFailed(nameof(Signalling), ex.Message);
            }
        }

        private async Task HandlePacket(SignallingPacket _packet, EndPoint _endPoint)
        {
            switch (_packet.PacketDataIdentifier)
            {
                case PacketIdentifier.Login:
                    var KeyResult = _packet.PacketLoginKey != null ? _packet.PacketLoginKey : "No Key";
                    Logger.LogToConsole(LogType.Info, $"Received Incoming Login: {KeyResult}", nameof(Signalling));
                    if (_packet.PacketVersion != MainEntry.Version)
                    {
                        await SendPacket(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Deny }, _endPoint);
                        return;
                    }

                    var participant = new Participant();
                    participant.SocketData.SignallingAddress = _endPoint;

                    var GeneratedKey = _packet.PacketLoginKey;

                    if (string.IsNullOrWhiteSpace(_packet.PacketLoginKey) || serverData.GetParticipantByKey(GeneratedKey) != null)
                    {
                        GeneratedKey = GenerateKey();
                        while (true)
                        {
                            //Make sure the new generated key does not conflict with existing keys
                            if (serverData.GetParticipantByKey(GeneratedKey) != null)
                                GeneratedKey = GenerateKey();
                            else
                                break;
                        }
                    }

                    participant.LoginKey = GeneratedKey;
                    serverData.AddParticipant(participant);
                    await SendPacket(new SignallingPacket()
                    {
                        PacketDataIdentifier = PacketIdentifier.Accept,
                        PacketLoginKey = GeneratedKey,
                        PacketVoicePort = ServerProperties._serverProperties.VoicePort_UDP
                    }, _endPoint);
                    break;

                case PacketIdentifier.Ping:
                    var participantPinger = serverData.GetParticipantBySignallingAddress(_endPoint);
                    if (participantPinger != null)
                    {
                        participantPinger.SocketData.LastPing = DateTime.UtcNow;
                        serverData.EditParticipant(participantPinger);
                        await SendPacket(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Ping }, _endPoint);
                    }
                    break;
            }
        }

        public async Task SendPacket(SignallingPacket _packet, EndPoint _endPoint)
        {
            await socket.SendToAsync(new ArraySegment<byte>(_packet.GetPacketDataStream()), SocketFlags.None, _endPoint);
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

        private async Task OnParticipantBinded(Participant participant)
        {
            await SendPacket(new SignallingPacket() { PacketDataIdentifier = PacketIdentifier.Binded, PacketName = participant.MinecraftData.Gamertag }, participant.SocketData.SignallingAddress);

            var list = serverData.GetParticipants().Where(x => x.Binded && x.LoginKey != participant.LoginKey);
            for (int i = 0; i < list.Count(); i++)
            {
                await SendPacket(new SignallingPacket()
                {
                    PacketDataIdentifier = PacketIdentifier.Login,
                    PacketLoginKey = list.ElementAt(i).LoginKey,
                    PacketName = list.ElementAt(i).MinecraftData.Gamertag
                }, participant.SocketData.SignallingAddress);
                await SendPacket(new SignallingPacket()
                {
                    PacketDataIdentifier = PacketIdentifier.Login,
                    PacketLoginKey = participant.LoginKey,
                    PacketName = participant.MinecraftData.Gamertag
                }, list.ElementAt(i).SocketData.SignallingAddress);
            }
        }

        private async Task OnParticipantLogout(Participant participant, string reason = null)
        {
            if (reason == "Server Request")
            {
                await SendPacket(new SignallingPacket()
                {
                    PacketDataIdentifier = PacketIdentifier.Logout,
                }, participant.SocketData.SignallingAddress);
            }

            if (participant.Binded)
            {
                var list = serverData.GetParticipants().Where(x => x.Binded && x.LoginKey != participant.LoginKey);
                for (int i = 0; i < list.Count(); i++)
                {
                    await SendPacket(new SignallingPacket()
                    {
                        PacketDataIdentifier = PacketIdentifier.Logout,
                        PacketLoginKey = participant.LoginKey
                    }, list.ElementAt(i).SocketData.SignallingAddress);
                }
            }
        }
    }
}
