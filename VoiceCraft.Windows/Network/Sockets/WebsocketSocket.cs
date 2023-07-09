using Fleck;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using VoiceCraft.Windows.Network.Builders;
using VoiceCraft.Windows.Network.Packets;
using VoiceCraft.Windows.Storage;

namespace VoiceCraft.Windows.Network.Sockets
{
    public class WebsocketSocket
    {
        //Variables
        private readonly NetworkManager NM;
        private readonly WebSocketServer Socket;
        private ushort SocketCount;
        private List<IWebSocketConnection> AllSockets = new List<IWebSocketConnection>();
        private readonly string[] Dimensions;
        private bool Binded;

        //Events
        public delegate void Connect(string Username);
        public delegate void Disconnect();

        public event Connect? OnConnect;
        public event Disconnect? OnDisconnect;

        public WebsocketSocket(NetworkManager NM)
        {
            this.NM = NM;
            var settings = Database.GetSettings();
            Socket = new WebSocketServer($"ws://0.0.0.0:{settings.WebsocketPort}");
            Dimensions = new string[] { "minecraft:overworld", "minecraft:nether", "minecraft:end" };
            Binded = false;
        }

        public void StartConnect()
        {
            Socket.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    AllSockets.Add(socket);
                    SocketCount++;
                    if (SocketCount <= 1)
                    {
                        socket.Send(new CommandBuilder().SetCommand("/getlocalplayername").Build());
                        socket.Send(new EventBuilder().SetEventType(EventType.PlayerTravelled).Build());
                    }
                    else
                        socket.Close();
                };

                socket.OnClose = () =>
                {
                    AllSockets.Remove(socket);
                    SocketCount--;
                    if (SocketCount <= 0)
                    {
                        OnDisconnect?.Invoke();
                        NM.Voice.SendPacket(new VoicePacket() { PacketIdentifier = VoicePacketIdentifier.UpdatePosition, PacketEnviromentId = "void" }.GetPacketDataStream());
                    }
                };

                socket.OnMessage = message =>
                {
                    var json = JObject.Parse(message);
                    if (!json.ContainsKey("header")) return;

                    if (json["header"]?["messagePurpose"]?.Value<string>() == "commandResponse")
                    {
                        var playerName = json["body"]?["localplayername"]?.Value<string>();
                        if (!string.IsNullOrWhiteSpace(playerName))
                        {
                            if (!Binded)
                            {
                                NM.Signalling.SendPacket(new SignallingPacket() { PacketIdentifier = SignallingPacketIdentifiers.Binded, PacketVersion = App.Version, PacketMetadata = playerName }.GetPacketDataStream());
                                Binded = true;
                            }
                            OnConnect?.Invoke(playerName);
                        }
                    }

                    else if (json["header"]?["messagePurpose"]?.Value<string>() == "event" && json["header"]?["eventName"]?.Value<string>() == "PlayerTravelled")
                    {
                        var x = json["body"]?["player"]?["position"]?["x"]?.Value<float>();
                        var y = json["body"]?["player"]?["position"]?["y"]?.Value<float>();
                        var z = json["body"]?["player"]?["position"]?["z"]?.Value<float>();
                        var dimensionInt = json["body"]?["dimension"]?.Value<int>();

                        NM.Voice.SendPacket(new VoicePacket() { PacketIdentifier = VoicePacketIdentifier.UpdatePosition, PacketPosition = new System.Numerics.Vector3(x ?? 0, y ?? 0, z ?? 0), PacketEnviromentId = Dimensions[dimensionInt ?? 0] }.GetPacketDataStream());
                        Debug.WriteLine($"PlayerTravelled: {x}, {y}, {z}, {Dimensions[dimensionInt ?? 0]}");
                    }
                };
            });
        }

        public void StartDisconnect()
        {
            foreach(var socket in AllSockets)
            {
                socket.Close();
            }

            AllSockets.Clear();
            Socket.Dispose();
        }
    }
}
