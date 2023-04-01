using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;

namespace VoiceCraft_Server.Servers
{
    public class MCComm
    {
        private HttpListener listener;
        private string sessionKey;

        //Events Here
        public delegate void Fail(string reason);
        public delegate void Bind(Participant participant);

        public event Fail OnFail;
        public static event Bind OnBind;
        
        public MCComm()
        {
            sessionKey = Guid.NewGuid().ToString();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{ServerProperties._serverProperties.MCCommPort_TCP}/");
            try
            {
                Logger.LogToConsole(LogType.Info, "Starting Server", nameof(MCComm));
                listener.Start();
                Logger.LogToConsole(LogType.Success, $"Server Started: Login Key - {sessionKey}", nameof(MCComm));

                listener.BeginGetContext(new AsyncCallback(listen), null);
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

                OnFail?.Invoke(ex.Message);
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
                    Console.WriteLine(content);
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
                        case PacketType.Bind:
                            var participant = ServerMetadata.voiceParticipants.FirstOrDefault(x => x.LoginId == json.PlayerKey);
                            if(participant != null)
                            {
                                participant.Binded = true;
                                participant.Name = json.Username;
                                SendResponse(ctx, HttpStatusCode.Accepted, "Successfully Binded");
                                var element = ServerMetadata.voiceParticipants.FindIndex(x => x.LoginId == json.PlayerKey);
                                ServerMetadata.voiceParticipants[element] = participant;

                                Logger.LogToConsole(LogType.Success, $"Successfully binded user: Username: {participant.Name}, Key: {participant.LoginId}", nameof(MCComm));
                                OnBind?.Invoke(participant);
                            }
                            else
                            {
                                SendResponse(ctx, HttpStatusCode.NotFound, "Could not find key. Binding Unsuccessfull");
                            }
                            break;

                        case PacketType.Update:
                            for (int i = 0; i < json.Players.Count; i++)
                            {
                                var player = json.Players[i];
                                var vcParticipant = ServerMetadata.voiceParticipants.FirstOrDefault(x => x.LoginId == player.PlayerKey);
                                if(vcParticipant != null)
                                {
                                    vcParticipant.Position = player.Location;
                                    vcParticipant.EnvId = player.EnviromentId;
                                    var element = ServerMetadata.voiceParticipants.FindIndex(x => x.LoginId == player.PlayerKey);
                                    ServerMetadata.voiceParticipants[element] = vcParticipant;
                                }
                            }

                            SendResponse(ctx, HttpStatusCode.OK, "Updated");
                            break;

                        case PacketType.Login:
                            SendResponse(ctx, HttpStatusCode.Accepted, "Accepted Login");
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
        public string PlayerKey { get; set; } = "";
        public string Username { get; set; } = "";
        public List<Player> Players { get; set; } = new List<Player>();
    }

    public class Player
    {
        public string PlayerKey { get; set; } = "";
        public string EnviromentId { get; set; } = "";
        public Vector3 Location { get; set; } = new Vector3();
    }

    public enum PacketType
    {
        Bind,
        Update,
        Login
    }
}
