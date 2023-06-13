using System.Net.Sockets;
using System.Net;
using VoiceCraft.Server.Helpers;
using VoiceCraft.Server.Network.Packets;

namespace VoiceCraft.Server.Sockets
{
    public partial class Voice
    {
        private readonly Socket VoiceSocket;
        private readonly EndPoint IPEndpoint;

        public Voice()
        {
            IPEndpoint = new IPEndPoint(IPAddress.Any, 0);
            VoiceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            ServerEvents.OnStopping += OnStopping;
        }

        private Task OnStopping()
        {
            VoiceSocket?.Close();
            VoiceSocket?.Dispose();
            return Task.CompletedTask;
        }

        public async Task Start()
        {
            try
            {
                Logger.LogToConsole(LogType.Info, $"Starting Server - Port: {ServerProperties.Properties.VoicePortUDP}", nameof(Voice));

                IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, ServerProperties.Properties.VoicePortUDP);
                VoiceSocket.Bind(serverEp);

                ServerEvents.InvokeStarted(nameof(Voice));

                while (true)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new(new byte[3076]);
                        SocketReceiveFromResult result = await VoiceSocket.ReceiveFromAsync(buffer, SocketFlags.None, IPEndpoint);
                        if (buffer.Array != null)
                        {
                            _ = Task.Run(() => {
                                var packet = new VoicePacket(buffer.Array);
                                HandlePacket(packet, result.RemoteEndPoint);
                            });
                        }
                    }
                    catch (SocketException ex) when (ex.Message == "The I/O operation has been aborted because of either a thread exit or an application request.")
                    {
                        break; //Break out on disconnect/close
                    }
                    catch (Exception ex)
                    {
                        Logger.LogToConsole(LogType.Error, ex.Message, nameof(Voice));
                    }
                }
            }
            catch (Exception ex)
            {
                ServerEvents.InvokeFailed(nameof(Voice), ex.Message);
            }
        }

        private void HandlePacket(VoicePacket Packet, EndPoint EP)
        {
            switch(Packet.PacketIdentifier)
            {
                case VoicePacketIdentifier.Login:
                    HandleLogin(Packet, EP);
                    break;
                case VoicePacketIdentifier.Audio:
                    HandleAudio(Packet, EP);
                    break;
                case VoicePacketIdentifier.UpdatePosition:
                    HandleUpdate(Packet, EP);
                    break;
            }
        }

        private async void SendPacket(VoicePacket Packet, EndPoint EP)
        {
            await VoiceSocket.SendToAsync(new ArraySegment<byte>(Packet.GetPacketDataStream()), SocketFlags.None, EP);
        }
    }
}
