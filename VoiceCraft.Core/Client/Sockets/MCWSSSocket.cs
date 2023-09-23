using Fleck;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Numerics;
using VoiceCraft.Core.Client.Builders;

namespace VoiceCraft.Windows.Network.Sockets
{
    public class MCWSSSocket : IDisposable
    {
        //Variables
        private WebSocketServer Socket;
        private IWebSocketConnection? ConnectedSocket;
        private readonly string[] Dimensions;
        private int Port;

        public bool IsConnected { get; private set; }
        public bool IsDisposed { get; private set; }

        //Events
        public delegate void Connect(string Username);
        public delegate void PlayerTravelled(Vector3 position, string Dimension);
        public delegate void Disconnect();

        public event Connect? OnConnect;
        public event PlayerTravelled? OnPlayerTravelled;
        public event Disconnect? OnDisconnect;

        public MCWSSSocket(int Port)
        {
            this.Port = Port;
            Socket = new WebSocketServer($"ws://0.0.0.0:{Port}");
            Dimensions = new string[] { "minecraft:overworld", "minecraft:nether", "minecraft:end" };
        }

        public void Start()
        {
            Socket.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    if (ConnectedSocket == null)
                    {
                        socket.Send(new CommandBuilder().SetCommand("/getlocalplayername").Build());
                        socket.Send(new EventBuilder().SetEventType(EventType.PlayerTravelled).Build());
                        ConnectedSocket = socket;
                        IsConnected = true;
                    }
                    else
                        socket.Close();
                };

                socket.OnClose = () =>
                {
                    if (socket == ConnectedSocket)
                    {
                        ConnectedSocket = null;
                        IsConnected = false;
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
            if (ConnectedSocket != null) ConnectedSocket.Close();
            ConnectedSocket = null;
            Socket.Dispose();
            Socket = new WebSocketServer($"ws://0.0.0.0:{Port}");
        }

        //Dispose Handlers
        ~MCWSSSocket()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Socket.Dispose();
                    IsConnected = false;
                }
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
