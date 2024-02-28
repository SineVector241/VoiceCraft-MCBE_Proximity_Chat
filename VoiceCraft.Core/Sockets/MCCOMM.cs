using System.Collections.Generic;
using System.IO;
using System;
using System.Net;
using System.Text;
using System.Threading;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCComm;

namespace VoiceCraft.Core.Sockets
{
    public class MCCOMM
    {
        private readonly HttpListener Listener;
        public string ServerKey { get; private set; } = string.Empty;
        public CancellationToken CT { get; }

        //Debug Settings
        public bool LogExceptions { get; set; } = false;
        public bool LogInbound { get; set; } = false;
        public bool LogOutbound { get; set; } = false;
        public List<MCCommPacketTypes> InboundFilter { get; set; } = new List<MCCommPacketTypes>();
        public List<MCCommPacketTypes> OutboundFilter { get; set; } = new List<MCCommPacketTypes>();

        //Delegates
        public delegate void PacketData<T>(T packet, HttpListenerContext ctx);

        public delegate void InboundPacket(MCCommPacket packet);
        public delegate void OutboundPacket(MCCommPacket packet);
        public delegate void ExceptionError(Exception error);

        //events
        public event PacketData<Login>? OnLoginPacketReceived;
        public event PacketData<Bind>? OnBindedPacketReceived;
        public event PacketData<Update>? OnUpdatePacketReceived;
        public event PacketData<UpdateSettings>? OnUpdateSettingsPacketReceived;
        public event PacketData<GetSettings>? OnGetSettingsPacketReceived;
        public event PacketData<RemoveParticipant>? OnRemoveParticipantPacketReceived;
        public event PacketData<ChannelMove>? OnChannelMovePacketReceived;

        public event InboundPacket? OnInboundPacket;
        public event OutboundPacket? OnOutboundPacket;
        public event ExceptionError? OnExceptionError;

        public MCCOMM(CancellationToken CT)
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
                        var packet = new MCCommPacket(content);
                        HandlePacket(packet, ctx);
                    }
                    catch (Exception ex)
                    {
                        if (LogExceptions)
                            OnExceptionError?.Invoke(ex);

                        SendResponse(ctx, HttpStatusCode.BadRequest, Deny.Create("Invalid Data!"));
                    }
                }
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                    OnExceptionError?.Invoke(ex);

                if (CT.IsCancellationRequested)
                    Stop();
            }
        }

        private void HandlePacket(MCCommPacket packet, HttpListenerContext ctx)
        {
            try
            {
                if (LogInbound && (InboundFilter.Count == 0 || InboundFilter.Contains(packet.PacketType)))
                    OnInboundPacket?.Invoke(packet);

                switch (packet.PacketType)
                {
                    case MCCommPacketTypes.Login:
                        var loginData = (Login)packet.PacketData;
                        if (loginData == null)
                        {
                            SendResponse(ctx, HttpStatusCode.OK, Deny.Create("Invalid Data!"));
                            break;
                        }
                        OnLoginPacketReceived?.Invoke(loginData, ctx);
                        break;
                    case MCCommPacketTypes.Bind:
                        var bindData = (Bind)packet.PacketData;
                        if (bindData == null)
                        {
                            SendResponse(ctx, HttpStatusCode.OK, Deny.Create("Invalid Data!"));
                            break;
                        }
                        OnBindedPacketReceived?.Invoke(bindData, ctx);
                        break;
                    case MCCommPacketTypes.Update:
                        var updateData = (Update)packet.PacketData;
                        if (updateData == null)
                        {
                            SendResponse(ctx, HttpStatusCode.OK, Deny.Create("Invalid Data!"));
                            break;
                        }
                        OnUpdatePacketReceived?.Invoke(updateData, ctx);
                        break;
                    case MCCommPacketTypes.UpdateSettings:
                        var updateSettingsData = (UpdateSettings)packet.PacketData;
                        if (updateSettingsData == null)
                        {
                            SendResponse(ctx, HttpStatusCode.OK, Deny.Create("Invalid Data!"));
                            break;
                        }
                        OnUpdateSettingsPacketReceived?.Invoke(updateSettingsData, ctx);
                        break;
                    case MCCommPacketTypes.GetSettings:
                        var getSettingsData = (GetSettings)packet.PacketData;
                        if (getSettingsData == null)
                        {
                            SendResponse(ctx, HttpStatusCode.OK, Deny.Create("Invalid Data!"));
                            break;
                        }
                        OnGetSettingsPacketReceived?.Invoke(getSettingsData, ctx);
                        break;
                    case MCCommPacketTypes.RemoveParticipant:
                        var removeParticipantData = (RemoveParticipant)packet.PacketData;
                        if (removeParticipantData == null)
                        {
                            SendResponse(ctx, HttpStatusCode.OK, Deny.Create("Invalid Data!"));
                            break;
                        }
                        OnRemoveParticipantPacketReceived?.Invoke(removeParticipantData, ctx);
                        break;
                    case MCCommPacketTypes.ChannelMove:
                        var channelMoveData = (ChannelMove)packet.PacketData;
                        if (channelMoveData == null)
                        {
                            SendResponse(ctx, HttpStatusCode.OK, Deny.Create("Invalid Data!"));
                            break;
                        }
                        OnChannelMovePacketReceived?.Invoke(channelMoveData, ctx);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (LogExceptions)
                {
                    OnExceptionError?.Invoke(ex);
                }
            }
        }

        public void SendResponse(HttpListenerContext ctx, HttpStatusCode code, MCCommPacket packet)
        {
            if (LogOutbound && (OutboundFilter.Count == 0 || OutboundFilter.Contains(packet.PacketType)))
                OnOutboundPacket?.Invoke(packet);

            var content = Encoding.UTF8.GetBytes(packet.GetPacketString());
            ctx.Response.StatusCode = (int)code;
            ctx.Response.ContentType = "text/plain";
            ctx.Response.OutputStream.Write(content, 0, content.Length);
            ctx.Response.OutputStream.Close();
        }
    }
}