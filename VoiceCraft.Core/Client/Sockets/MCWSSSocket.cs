using Fleck;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using VoiceCraft.Core.Client.Builders;

namespace VoiceCraft.Windows.Network.Sockets
{
    public class MCWSSSocket
    {
        //Variables
        private WebSocketServer? Socket;
        private ushort SocketCount;
        private List<IWebSocketConnection> AllSockets = new List<IWebSocketConnection>();
        private readonly string[] Dimensions;

        //Events
        public delegate void Connect(string Username);
        public delegate void PlayerTravelled(Vector3 position, string Dimension);
        public delegate void Disconnect();

        public event Connect? OnConnect;
        public event PlayerTravelled? OnPlayerTravelled;
        public event Disconnect? OnDisconnect;

        public MCWSSSocket(int Port)
        {
            Socket = new WebSocketServer($"ws://0.0.0.0:{Port}");
            Dimensions = new string[] { "minecraft:overworld", "minecraft:nether", "minecraft:end" };
            SocketCount = 0;
        }

        public void Start()
        {
            Socket?.Start(socket =>
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
                            OnConnect?.Invoke(playerName);
                        }
                    }

                    else if (json["header"]?["messagePurpose"]?.Value<string>() == "event" && json["header"]?["eventName"]?.Value<string>() == "PlayerTravelled")
                    {
                        var x = json["body"]?["player"]?["position"]?["x"]?.Value<float>();
                        var y = json["body"]?["player"]?["position"]?["y"]?.Value<float>();
                        var z = json["body"]?["player"]?["position"]?["z"]?.Value<float>();
                        var dimensionInt = json["body"]?["dimension"]?.Value<int>();

                        OnPlayerTravelled?.Invoke(new Vector3(x ?? 0, y ?? 0, z ?? 0), Dimensions[dimensionInt ?? 0]);
#if DEBUG
                        Debug.WriteLine($"PlayerTravelled: {x}, {y}, {z}, {Dimensions[dimensionInt ?? 0]}");
#endif
                    }
                };
            });
        }

        public void Stop()
        {
            foreach(var socket in AllSockets)
            {
                socket.Close();
            }

            AllSockets.Clear();
            Socket?.Dispose();
            Socket = null;
        }
    }
}
