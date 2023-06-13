using System.Net;
using System.Net.Sockets;
using VoiceCraft.Server.Helpers;
using VoiceCraft.Server.Network.Packets;

namespace VoiceCraft.Server.Sockets
{
    public partial class Signalling
    {
        private readonly Socket SignallingSocket;
        private readonly EndPoint IPEndpoint;

        public Signalling()
        {
            IPEndpoint = new IPEndPoint(IPAddress.Any, 0);
            SignallingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            ServerEvents.OnParticipantBinded += OnParticipantBinded;
            ServerEvents.OnParticipantLogout += OnParticipantLogout;
            ServerEvents.OnStopping += OnStopping;
        }

        private Task OnStopping()
        {
            SignallingSocket?.Close();
            SignallingSocket?.Dispose();
            return Task.CompletedTask;
        }

        public async Task Start()
        {
            try
            {
                Logger.LogToConsole(LogType.Info, $"Starting Server - Port: {ServerProperties.Properties.SignallingPortUDP}", nameof(Signalling));

                IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, ServerProperties.Properties.SignallingPortUDP);
                SignallingSocket.Bind(serverEp);

                ServerEvents.InvokeStarted(nameof(Signalling));

                while (true)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new(new byte[2048]);
                        SocketReceiveFromResult result = await SignallingSocket.ReceiveFromAsync(buffer, SocketFlags.None, IPEndpoint);
                        if (buffer.Array != null)
                        {
                            var packet = new SignallingPacket(buffer.Array);
                            HandlePacket(packet, result.RemoteEndPoint);
                        }
                    }
                    catch(SocketException ex) when(ex.Message == "The I/O operation has been aborted because of either a thread exit or an application request.")
                    {
                        break; //Break out on disconnect/close
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

        private void HandlePacket(SignallingPacket Packet, EndPoint EP)
        {
            if(Packet.PacketVersion != MainEntry.Version)
            {
                SendPacket(new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Deny, PacketMetadata = "Client version does not match server version!" }, EP);
                return;
            }

            if (ServerProperties.Banlist.IPBans.Exists(x => x == EP?.ToString()?.Split(':').FirstOrDefault()))
            {
                SendPacket(new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Deny, PacketMetadata = "You are banned from this server!" }, EP);
                return;
            }

            switch (Packet.PacketIdentifier)
            {
                case SignallingPacketIdentifiers.InfoPing:
                    HandleInfoPing(EP);
                    break;
                case SignallingPacketIdentifiers.Ping:
                    HandlePing(EP);
                    break;
                case SignallingPacketIdentifiers.LoginServerSided:
                    HandleServerSidedLogin(Packet, EP);
                    break;
                case SignallingPacketIdentifiers.LoginClientSided:
                    HandleClientSidedLogin(Packet, EP);
                    break;
                case SignallingPacketIdentifiers.Logout:
                    HandleLogout(EP);
                    break;
                case SignallingPacketIdentifiers.Binded:
                    HandleBind(Packet, EP);
                    break;
            }
        }

        private async void SendPacket(SignallingPacket Packet, EndPoint EP)
        {
            await SignallingSocket.SendToAsync(new ArraySegment<byte>(Packet.GetPacketDataStream()), SocketFlags.None, EP);
        }

        private Task OnParticipantBinded(Participant Participant, ushort Key)
        {
            if(Participant.SocketData.SignallingAddress != null)
                SendPacket(new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Binded, PacketMetadata = Participant.MinecraftData.Gamertag }, Participant.SocketData.SignallingAddress);

            var list = ServerData.Participants.Where(x => x.Value != null && x.Value.Binded && x.Key != Key);

            for (int i = 0; i < list.Count(); i++)
            {
                var externalParticipant = list.ElementAt(i);
                if (Participant.SocketData.SignallingAddress != null && 
                    externalParticipant.Value != null && 
                    externalParticipant.Value.SocketData.SignallingAddress != null)
                {
                    SendPacket(new SignallingPacket()
                    {
                        PacketIdentifier = SignallingPacketIdentifiers.Login,
                        PacketKey = externalParticipant.Key,
                        PacketMetadata = externalParticipant.Value.MinecraftData.Gamertag,
                        PacketCodec = externalParticipant.Value.Codec
                    }, Participant.SocketData.SignallingAddress);

                    SendPacket(new SignallingPacket()
                    {
                        PacketIdentifier = SignallingPacketIdentifiers.Login,
                        PacketKey = Key,
                        PacketMetadata = Participant.MinecraftData.Gamertag,
                        PacketCodec = Participant.Codec
                    }, externalParticipant.Value.SocketData.SignallingAddress);
                }
            }

            return Task.CompletedTask;
        }

        private Task OnParticipantLogout(Participant Participant, ushort Key, string? reason)
        {
            foreach(var participant in ServerData.Participants.Values)
            {
                if (participant != null)
                {
                    var socketData = participant.SocketData.SignallingAddress;
                    if (participant.Binded && socketData != null)
                        SendPacket(new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Logout, PacketKey = Key }, socketData);
                }
            }
            return Task.CompletedTask;
        }
    }
}
