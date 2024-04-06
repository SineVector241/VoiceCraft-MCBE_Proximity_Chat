using System.Collections.Concurrent;
using System.Net;
using System.Text;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCComm;

namespace VoiceCraft.Network.Sockets
{
    public class MCComm
    {
        #region Variables
        private HttpListener WebServer = new HttpListener();
        public string LoginKey { get; private set; } = string.Empty;
        public PacketRegistry PacketRegistry { get; set; } = new PacketRegistry();
        public ConcurrentDictionary<string, long> Sessions { get; set; } = new ConcurrentDictionary<string, long>();
        public int Timeout { get; set; } = 8000;
        private Task? ActivityChecker { get; set; }
        #endregion

        #region Debug Variables
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<MCCommPacketTypes> InboundFilter { get; set; } = new List<MCCommPacketTypes>();
        public List<MCCommPacketTypes> OutboundFilter { get; set; } = new List<MCCommPacketTypes>();
        #endregion

        #region Delegates
        public delegate void Started();
        public delegate void Stopped(string? reason = null);
        public delegate void ServerConnected(string token, string address);
        public delegate void ServerDisconnected(int timeoutDiff, string token);
        public delegate void PacketData<T>(T packet, HttpListenerContext ctx);

        public delegate void InboundPacket(MCCommPacket packet);
        public delegate void OutboundPacket(MCCommPacket packet);
        public delegate void ExceptionError(Exception error);
        #endregion

        #region Events
        public event Started? OnStarted;
        public event Stopped? OnStopped;
        public event ServerConnected? OnServerConnected;
        public event ServerDisconnected? OnServerDisconnected;

        public event PacketData<Login>? OnLoginReceived;
        public event PacketData<Accept>? OnAcceptReceived;
        public event PacketData<Deny>? OnDenyReceived;
        public event PacketData<Bind>? OnBindReceived;
        public event PacketData<Update>? OnUpdateReceived;
        public event PacketData<UpdateSettings>? OnUpdateSettingsReceived;
        public event PacketData<GetSettings>? OnGetSettingsReceived;
        public event PacketData<RemoveParticipant>? OnRemoveParticipantReceived;
        public event PacketData<ChannelMove>? OnChannelMoveReceived;
        public event PacketData<AckUpdate>? OnAckUpdateReceived;

        public event InboundPacket? OnInboundPacket;
        public event OutboundPacket? OnOutboundPacket;
        public event ExceptionError? OnExceptionError;
        #endregion

        public MCComm()
        {
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Login, typeof(Login));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Accept, typeof(Accept));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Deny, typeof(Deny));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Bind, typeof(Bind));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.Update, typeof(Update));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.UpdateSettings, typeof(UpdateSettings));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.GetSettings, typeof(GetSettings));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.RemoveParticipant, typeof(RemoveParticipant));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.ChannelMove, typeof(ChannelMove));
            PacketRegistry.RegisterPacket((byte)MCCommPacketTypes.AckUpdate, typeof(AckUpdate));
        }

        public async Task Start(ushort Port, string LoginKey)
        {
            try
            {
                this.LoginKey = LoginKey;
                WebServer.Prefixes.Add($"http://*:{Port}/");
                WebServer.Start();
                OnLoginReceived += LoginReceived;
                ActivityChecker = Task.Run(ActivityCheck);
                OnStarted?.Invoke();
                await ListenAsync();
            }
            catch (Exception ex)
            {
                OnStopped?.Invoke(ex.Message);
            }
        }

        public void Stop()
        {
            if (WebServer.IsListening)
            {
                WebServer.Stop();
                OnLoginReceived -= LoginReceived;
                Sessions.Clear();
                ActivityChecker = null;
            }
        }

        public void SendResponse(HttpListenerContext ctx, HttpStatusCode code, MCCommPacket packet)
        {
            if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains((MCCommPacketTypes)packet.PacketId)))
                OnOutboundPacket?.Invoke(packet);

            var content = Encoding.UTF8.GetBytes(packet.SerializePacket());
            ctx.Response.StatusCode = (int)code;
            ctx.Response.ContentType = "text/plain";
            ctx.Response.OutputStream.Write(content, 0, content.Length);
            ctx.Response.OutputStream.Close();
        }

        private async Task ListenAsync()
        {
            while (WebServer.IsListening)
            {
                try
                {
                    var ctx = await WebServer.GetContextAsync();

                    if (ctx.Request.HttpMethod == HttpMethod.Post.Method)
                    {
                        var content = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                        var packet = PacketRegistry.GetPacketFromJsonString(content);

                        if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains((MCCommPacketTypes)packet.PacketId)))
                            OnInboundPacket?.Invoke(packet);

                        HandlePacket(packet, ctx);
                    }
                }
                catch (Exception ex)
                {
                    if (LogExceptions)
                        OnExceptionError?.Invoke(ex);
                }
            }
        }

        private void HandlePacket(MCCommPacket packet, HttpListenerContext ctx)
        {
            try
            {
                if(packet.PacketId != (byte)MCCommPacketTypes.Login)
                {
                    if (Sessions.TryGetValue(packet.Token, out var session))
                        Sessions.TryUpdate(packet.Token, Environment.TickCount64, session);
                    else
                    {
                        SendResponse(ctx, HttpStatusCode.OK, new Deny() { Reason = "Invalid Token!" });
                        return;
                    }
                }

                switch ((MCCommPacketTypes)packet.PacketId)
                {
                    case MCCommPacketTypes.Login: OnLoginReceived?.Invoke((Login)packet, ctx); break;
                    case MCCommPacketTypes.Accept: OnAcceptReceived?.Invoke((Accept)packet, ctx); break;
                    case MCCommPacketTypes.Deny: OnDenyReceived?.Invoke((Deny)packet, ctx); break;
                    case MCCommPacketTypes.Update: OnUpdateReceived?.Invoke((Update)packet, ctx); break;
                    case MCCommPacketTypes.UpdateSettings: OnUpdateSettingsReceived?.Invoke((UpdateSettings)packet, ctx); break;
                    case MCCommPacketTypes.GetSettings: OnGetSettingsReceived?.Invoke((GetSettings)packet, ctx); break;
                    case MCCommPacketTypes.RemoveParticipant: OnRemoveParticipantReceived?.Invoke((RemoveParticipant)packet, ctx); break;
                    case MCCommPacketTypes.ChannelMove: OnChannelMoveReceived?.Invoke((ChannelMove)packet, ctx); break;
                    case MCCommPacketTypes.AckUpdate: OnAckUpdateReceived?.Invoke((AckUpdate)packet, ctx); break;
                }
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);

                SendResponse(ctx, HttpStatusCode.BadRequest, new Deny() { Reason = "Invalid Data!" });
            }
        }

        private void LoginReceived(Login packet, HttpListenerContext ctx)
        {
            if (packet.LoginKey == LoginKey)
            {
                var token = Guid.NewGuid().ToString();
                Sessions.TryAdd(token, Environment.TickCount64);
                SendResponse(ctx, HttpStatusCode.OK, new Accept() { Token = token });
                OnServerConnected?.Invoke(token, ctx.Request.RemoteEndPoint.Address.ToString());
            }
            else
            {
                SendResponse(ctx, HttpStatusCode.OK, new Deny() { Reason = "Invalid Login Key!" });
            }
        }

        private async Task ActivityCheck()
        {
            while(WebServer.IsListening)
            {
                foreach(var session in Sessions)
                {
                    if(Environment.TickCount64 - session.Value > Timeout && Sessions.TryRemove(session))
                    {
                        OnServerDisconnected?.Invoke((int)(Environment.TickCount64 - session.Value), session.Key);
                    }
                }

                await Task.Delay(1); //1ms to not destroy the cpu.
            }
        }
    }
}
