using Fleck;
using System.Numerics;
using System;
using System.Diagnostics;
using VoiceCraft.Core.Packets.MCWSS;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Builders;

namespace VoiceCraft.Core.Sockets
{
    public class MCWSS
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

        public MCWSS(int Port)
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
                    var packet = new MCWSSPacket(message);

                    if (packet.Header.messagePurpose == "commandResponse")
                    {
                        var data = (LocalPlayerNameResponse)packet.Body;
                        var name = data.localplayername;
                        OnConnect?.Invoke(name);
                    }

                    else if (packet.Header.messagePurpose == "event" && packet.Header.eventName == "PlayerTravelled")
                    {
                        var data = (PlayerTravelledEvent)packet.Body;
                        var x = data.player.position.x;
                        var y = data.player.position.y;
                        var z = data.player.position.z;
                        var dimensionInt = data.player.dimension;

                        OnPlayerTravelled?.Invoke(new Vector3(x, y, z), Dimensions[dimensionInt]);
#if DEBUG
                        Debug.WriteLine($"PlayerTravelled: {x}, {y}, {z}, {Dimensions[dimensionInt]}");
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
        ~MCWSS()
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
