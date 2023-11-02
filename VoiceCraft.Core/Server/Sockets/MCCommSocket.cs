using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading;

namespace VoiceCraft.Core.Server.Sockets
{
    public class MCCommSocket
    {
        private readonly HttpListener Listener;
        private string ServerKey = string.Empty;
        public CancellationToken CT { get; }

        //Debug Settings
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public List<PacketType> InboundFilter { get; set; } = new List<PacketType>();

        //Delegates
        public delegate void Started();
        public delegate void BindedPacket(WebserverPacket packet, HttpListenerContext ctx);
        public delegate void UpdatePacket(WebserverPacket packet, HttpListenerContext ctx);
        public delegate void UpdateSettingsPacket(WebserverPacket packet, HttpListenerContext ctx);
        public delegate void GetSettingsPacket(WebserverPacket packet, HttpListenerContext ctx);
        public delegate void RemoveParticipantPacket(WebserverPacket packet, HttpListenerContext ctx);

        public delegate void InboundPacket(WebserverPacket packet);
        public delegate void ExceptionError(Exception error);

        //events
        public event Started? OnStarted;
        public event BindedPacket? OnBindedPacketReceived;
        public event UpdatePacket? OnUpdatePacketReceived;
        public event UpdateSettingsPacket? OnUpdateSettingsPacketReceived;
        public event GetSettingsPacket? OnGetSettingsPacketReceived;
        public event RemoveParticipantPacket? OnRemoveParticipantPacketReceived;

        public event InboundPacket? OnInboundPacket;
        public event ExceptionError? OnExceptionError;

        public MCCommSocket(CancellationToken CT)
        {
            this.CT = CT;
            Listener = new HttpListener();
        }

        public void Start(ushort Port, string ServerKey)
        {
            this.ServerKey = ServerKey;
            Listener.Prefixes.Add($"http://*:{Port}/");
            Listener.Start();
            Listener.BeginGetContext(new AsyncCallback(Listen), null);
            OnStarted?.Invoke();
        }

        public void Stop()
        {
            Listener.Stop();
            Listener.Close();
        }

        private void Listen(IAsyncResult result)
        {
            try
            {
                CT.ThrowIfCancellationRequested();
                var ctx = Listener.EndGetContext(result);
                Listener.BeginGetContext(new AsyncCallback(Listen), null);
                if (ctx.Request.HttpMethod == "POST")
                {
                    try
                    {
                        var content = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                        var json = JsonConvert.DeserializeObject<WebserverPacket>(content);

                        //If json is not null and key matches then continue. If one or the other is invalid it will respond differently to each one and return.
                        if (json == null)
                        {
                            SendResponse(ctx, HttpStatusCode.BadRequest, "Invalid Content");
                            return;
                        }

                        if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains(json.Type)))
                            OnInboundPacket?.Invoke(json);

                        if (json.LoginKey != ServerKey)
                        {
                            SendResponse(ctx, HttpStatusCode.Forbidden, "Invalid Login Key");
                            return;
                        }

                        switch (json.Type)
                        {
                            case PacketType.Login:
                                SendResponse(ctx, HttpStatusCode.OK, "Key Accepted");
                                break;

                            case PacketType.Bind:
                                OnBindedPacketReceived?.Invoke(json, ctx);
                                break;

                            case PacketType.Update:
                                OnUpdatePacketReceived?.Invoke(json, ctx);
                                break;

                            case PacketType.UpdateSettings:
                                OnUpdateSettingsPacketReceived?.Invoke(json, ctx);
                                break;

                            case PacketType.GetSettings:
                                OnGetSettingsPacketReceived?.Invoke(json, ctx);
                                break;

                            case PacketType.RemoveParticipant:
                                OnRemoveParticipantPacketReceived?.Invoke(json, ctx);
                                break;
                            default:
                                break;
                        }

                    }
                    catch
                    {
                        SendResponse(ctx, HttpStatusCode.BadRequest, "Invalid Content");
                    }
                }
            }
            catch(Exception ex)
            {
                if(LogExceptions)
                    OnExceptionError?.Invoke(ex);

                if (CT.IsCancellationRequested)
                    Stop();
            }
        }

        public void SendResponse(HttpListenerContext ctx, HttpStatusCode code, string Content)
        {
            var content = Encoding.UTF8.GetBytes(Content);
            ctx.Response.StatusCode = (int)code;
            ctx.Response.ContentType = "text/plain";
            ctx.Response.OutputStream.Write(content, 0, content.Length);
            ctx.Response.OutputStream.Close();
        }
    }
    public class WebserverPacket
    {
        public PacketType Type { get; set; }
        public string LoginKey { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public ushort PlayerKey { get; set; }
        public string Gamertag { get; set; } = "";

        public List<Player> Players { get; set; } = new List<Player>();
        public ServerSettings Settings { get; set; } = new ServerSettings();
    }

    public class Player
    {
        public string PlayerId { get; set; } = "";
        public string DimensionId { get; set; } = "";
        public Vector3 Location { get; set; } = new Vector3();
        public float Rotation { get; set; }
        public float CaveDensity { get; set; }
        public bool IsDead { get; set; }
        public bool InWater { get; set; }
    }

    public class ServerSettings
    {
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = false;
        public bool VoiceEffects { get; set; } = false;
    }

    public enum PacketType
    {
        Login,
        Bind,
        Update,
        UpdateSettings,
        GetSettings,
        RemoveParticipant
    }
}