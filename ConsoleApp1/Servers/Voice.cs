using System.Net.Sockets;
using System.Net;
using VoiceCraft_Server.Data;
using System;
using System.Threading.Tasks;
using VCVoice_Packet;
using System.Linq;
using System.Numerics;

namespace VoiceCraft_Server.Servers
{
    public class Voice
    {
        private ServerData serverData;
        private Socket socket;
        private EndPoint endPoint;

        public Voice(ServerData serverDataObject)
        {
            try
            {
                serverData = serverDataObject;
                endPoint = new IPEndPoint(IPAddress.Any, 0);

                //Start binding port and udp server
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }
            catch(Exception ex)
            {
                ServerEvents.InvokeFailed(nameof(Voice), ex.Message);
            }
        }

        public async Task Start()
        {
            try
            {
                Logger.LogToConsole(LogType.Info, "Starting Server", nameof(Voice));

                IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, ServerProperties._serverProperties.VoicePort_UDP);
                socket.Bind(serverEp);

                ServerEvents.InvokeStarted(nameof(Voice));

                while (true)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[16024]);
                        SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, endPoint);
                        var packet = new VoicePacket(buffer.Array);

                        //Voice stuff works a lot more so this is probably better to handle with.
                        _ = Task.Factory.StartNew(async () =>
                        {
                            await HandlePacket(packet, result.RemoteEndPoint);
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogToConsole(LogType.Error, ex.Message, nameof(Voice));
                    }
                }
            }
            catch(Exception ex)
            {
                ServerEvents.InvokeFailed(nameof(Voice), ex.Message);
            }
        }

        private async Task HandlePacket(VoicePacket _packet, EndPoint _endPoint)
        {
            switch (_packet.PacketDataIdentifier)
            {
                case PacketIdentifier.Login:
                    Logger.LogToConsole(LogType.Info, $"New Login: Key: {_packet.PacketLoginKey}, Version: {_packet.PacketVersion}", nameof(Voice));
                    var VoiceParticipant = serverData.GetParticipantByKey(_packet.PacketLoginKey);
                    if (_packet.PacketVersion != MainEntry.Version)
                        await SendPacket(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Deny }, _endPoint);
                    else
                    {
                        if (string.IsNullOrWhiteSpace(_packet.PacketLoginKey))
                            await SendPacket(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Deny }, _endPoint);
                        else if (VoiceParticipant != null)
                        {
                            VoiceParticipant.SocketData.LastPing = DateTime.UtcNow;
                            VoiceParticipant.SocketData.VoiceAddress = _endPoint;
                            await SendPacket(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Accept }, _endPoint);
                            Logger.LogToConsole(LogType.Success, $"Accepted Login: Key: {_packet.PacketLoginKey}, Version: {_packet.PacketVersion}", nameof(Voice));
                        }
                        else
                            await SendPacket(new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Deny }, _endPoint);
                    }
                    break;

                case PacketIdentifier.Audio:
                    var Participant = serverData.GetParticipantByVoiceAddress(_endPoint);
                    if(Participant != null && Participant.Binded && !Participant.Muted)
                    {
                        var list = serverData.GetParticipants().Where(x => x.Binded && x.LoginKey != Participant.LoginKey && x.MinecraftData.DimensionId == Participant.MinecraftData.DimensionId && Vector3.Distance(x.MinecraftData.Position, Participant.MinecraftData.Position) <= ServerProperties._serverProperties.ProximityDistance);
                        for (int i = 0; i < list.Count(); i++)
                        {
                            if (list.ElementAt(i).SocketData.VoiceAddress != null)
                            {
                                var volume = 1 - (Vector3.Distance(list.ElementAt(i).MinecraftData.Position, Participant.MinecraftData.Position) / ServerProperties._serverProperties.ProximityDistance);
                                var rotationSource = Math.Atan2(list.ElementAt(i).MinecraftData.Position.X - Participant.MinecraftData.Position.X, list.ElementAt(i).MinecraftData.Position.Z - Participant.MinecraftData.Position.Z) - (Participant.MinecraftData.Rotation * -1 * (Math.PI / 180));
                                var packet = new VoicePacket() { PacketDataIdentifier = PacketIdentifier.Audio, PacketLoginKey = Participant.LoginKey, PacketAudio = _packet.PacketAudio, PacketBytesRecorded = _packet.PacketBytesRecorded, PacketVolume = volume, PacketRotationSource = (float)rotationSource };
                                await SendPacket(packet, list.ElementAt(i).SocketData.VoiceAddress);
                            }
                        }
                    }
                    break;
            }
        }

        private async Task SendPacket(VoicePacket _packet, EndPoint _endPoint)
        {
            await socket.SendToAsync(new ArraySegment<byte>(_packet.GetPacketDataStream()), SocketFlags.None, _endPoint);
        }
    }
}
