using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Tasks;
using VCVoice_Packet;

namespace VoiceCraft_Server.Servers
{
    public class Voice
    {
        private Socket serverSocket;
        private EndPoint endPoint;
        public Voice()
        {
            Logger.LogToConsole(LogType.Info, $"Starting Voice Server on port {ServerProperties._serverProperties.VoicePort_UDP}", nameof(Voice));

            endPoint = new IPEndPoint(IPAddress.Any, 0);

            //Bind Server to Address and Port
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, ServerProperties._serverProperties.VoicePort_UDP);
            serverSocket.Bind(serverEp);

        }

        public async Task StartServer()
        {
            Logger.LogToConsole(LogType.Success, "Voice server successfully initialised.", nameof(Voice));
            while (true)
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[2024]);
                SocketReceiveFromResult result = await serverSocket.ReceiveFromAsync(buffer, SocketFlags.None, endPoint);
                var packet = new VoicePacket(buffer.Array);
                HandlePacket(packet, result.RemoteEndPoint);
            }
        }

        private async void HandlePacket(VoicePacket _packet, EndPoint _endPoint)
        {
            switch (_packet.PacketDataIdentifier)
            {
                case PacketIdentifier.Login:
                    var VoiceParticipant = ServerMetadata.voiceParticipants.FirstOrDefault(x => x.LoginId == _packet.PacketLoginId);
                    if (_packet.PacketVersion != MainEntry.Version)
                        await serverSocket.SendToAsync(new ArraySegment<byte>(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Deny }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                    else
                    {
                        if (string.IsNullOrWhiteSpace(_packet.PacketLoginId))
                            await serverSocket.SendToAsync(new ArraySegment<byte>(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Deny }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                        else if (VoiceParticipant != null && VoiceParticipant.Binded)
                        {
                            VoiceParticipant.LastPing = DateTime.UtcNow;
                            VoiceParticipant.VoiceAddress = _endPoint;
                            await serverSocket.SendToAsync(new ArraySegment<byte>(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Accept }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                        }
                        else
                            await serverSocket.SendToAsync(new ArraySegment<byte>(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Deny }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                    }
                    break;

                case PacketIdentifier.Audio:
                    var Participant = ServerMetadata.voiceParticipants.FirstOrDefault(x => x.VoiceAddress == _endPoint && x.Binded == true);
                    if(Participant != null)
                    {
                        var list = ServerMetadata.voiceParticipants.Where(x => x.Binded == true && x.LoginId != Participant.LoginId).ToList();
                        for(int i = 0; i < list.Count; i++)
                        {
                            if (Vector3.Distance(list[i].Position, Participant.Position) <= 30)
                            {
                                var volume = Vector3.Distance(list[i].Position, Participant.Position) / 30;
                                await serverSocket.SendToAsync(new ArraySegment<byte>(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Audio, PacketLoginId = _packet.PacketLoginId, PacketAudio = _packet.PacketAudio, PacketVolume = volume }.GetPacketDataStream()), SocketFlags.None, _endPoint);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
