using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using VoiceCraft_Server.Data;

namespace VoiceCraft_Server.Servers
{
    public class MCComm
    {
        private HttpListener listener;
        private ServerData serverData;
        private string sessionKey;
        
        public MCComm(ServerData serverDataObject)
        {
            serverData = serverDataObject;

            if (string.IsNullOrWhiteSpace(ServerProperties._serverProperties.PermanentServerKey))
            {
                Logger.LogToConsole(LogType.Warn, "Permanent server key is empty. Generating Temporary key!", nameof(MCComm));
                sessionKey = Guid.NewGuid().ToString();
            }
            else
            {
                Logger.LogToConsole(LogType.Warn, $"Permanent server key found. Using permanent server key!", nameof(MCComm));
                sessionKey = ServerProperties._serverProperties.PermanentServerKey;
            }

            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{ServerProperties._serverProperties.MCCommPort_TCP}/");
            try
            {
                Logger.LogToConsole(LogType.Info, "Starting Server", nameof(MCComm));
                listener.Start();
                listener.BeginGetContext(new AsyncCallback(listen), null);

                Logger.LogToConsole(LogType.Info, $"Server Key: {sessionKey}", nameof(MCComm));
                ServerEvents.InvokeStarted(nameof(MCComm));
            }
            catch (HttpListenerException ex)
            {
                if (ex.ErrorCode == 5)
                {
                    var username = Environment.GetEnvironmentVariable("USERNAME");
                    var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error. Could not start server: Missing Permissions");
                    Console.WriteLine($"Missing permissions to listen on http://*:{ServerProperties._serverProperties.MCCommPort_TCP}/\n");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Please give access by typing in the following command in a command prompt\nnetsh http add urlacl url=http://*:{ServerProperties._serverProperties.MCCommPort_TCP}/ user={userdomain}\\{username} listen=yes\nAnd then restart the server\n");
                    Console.WriteLine("Or run this application as an Administrator.");
                    Console.ResetColor();
                }

                ServerEvents.InvokeFailed(nameof(MCComm), ex.Message);
            }
        }

        private void listen(IAsyncResult result)
        {
            var ctx = listener.EndGetContext(result);
            listener.BeginGetContext(new AsyncCallback(listen), null);
            if (ctx.Request.HttpMethod == "POST")
            {
                try
                {
                    var content = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                    var json = JsonConvert.DeserializeObject<WebserverPacket>(content);

                    //If json is not null and key matches then continue. If one or the other is invalid it will respond differently to each one and return.
                    if(json == null)
                    {
                        SendResponse(ctx, HttpStatusCode.BadRequest, "Invalid Content");
                        return;
                    }
                    else if(json.LoginKey != sessionKey)
                    {
                        SendResponse(ctx, HttpStatusCode.Forbidden, "Invalid Login Key");
                        return;
                    }

                    switch(json.Type)
                    {
                        case PacketType.Login:
                            SendResponse(ctx, HttpStatusCode.OK, "Key Accepted");
                            break;

                        case PacketType.Bind:
                            var participant = serverData.GetParticipantByKey(json.PlayerKey);
                            if(participant != null && !participant.Binded && !serverData.GetParticipants().Exists(x => x.MinecraftData.PlayerId == json.PlayerId))
                            {
                                participant.Binded = true;
                                participant.MinecraftData.Gamertag = json.Gamertag;
                                participant.MinecraftData.PlayerId = json.PlayerId;
                                SendResponse(ctx, HttpStatusCode.Accepted, "Successfully Binded");
                                serverData.EditParticipant(participant);
                                ServerEvents.InvokeParticipantBinded(participant);
                            }
                            else
                            {
                                SendResponse(ctx, HttpStatusCode.NotFound, "Could not find key or participant is already binded!");
                            }
                            break;

                        case PacketType.Update:
                            for (int i = 0; i < json.Players.Count; i++)
                            {
                                var player = json.Players[i];
                                var vcParticipant = serverData.GetParticipants().FirstOrDefault(x => x.MinecraftData.PlayerId == player.PlayerId);
                                if(vcParticipant != null)
                                {
                                    vcParticipant.MinecraftData.Position = player.Location;
                                    vcParticipant.MinecraftData.DimensionId = player.DimensionId;
                                    vcParticipant.MinecraftData.Rotation = player.Rotation;
                                }
                            }

                            SendResponse(ctx, HttpStatusCode.OK, "Updated");
                            break;

                        case PacketType.RemoveParticipant:
                            var mcParticipant = serverData.GetParticipantByMinecraftId(json.PlayerId);
                            if(mcParticipant != null)
                            {
                                serverData.RemoveParticipant(mcParticipant, true);
                                SendResponse(ctx, HttpStatusCode.OK, "Removed");
                            }
                            else
                            {
                                SendResponse(ctx, HttpStatusCode.NotFound, "Could Not Find Participant");
                            }
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

        private void SendResponse(HttpListenerContext ctx, HttpStatusCode code, string Content)
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
        public string PlayerKey { get; set; } = "";
        public string Gamertag { get; set; } = "";

        public List<Player> Players { get; set; } = new List<Player>();
    }

    public class Player
    {
        public string PlayerId { get; set; } = "";
        public string DimensionId { get; set; } = "";
        public Vector3 Location { get; set; } = new Vector3();
        public float Rotation { get; set; }
    }

    public enum PacketType
    {
        Login,
        Bind,
        Update,
        RemoveParticipant
    }
}
