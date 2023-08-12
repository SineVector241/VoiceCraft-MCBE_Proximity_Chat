using Newtonsoft.Json;
using System.Net;
using System.Numerics;
using System.Text;
using VoiceCraft.Server.Helpers;

namespace VoiceCraft.Server.Sockets
{
    public class MCComm
    {
        private readonly HttpListener Listener;
        
        public MCComm()
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://*:{ServerProperties.Properties.MCCommPortTCP}/");
            ServerEvents.OnStopping += OnStopping;
        }

        private Task OnStopping()
        {
            try
            {
                Listener?.Stop();
            }
            catch(ObjectDisposedException) { 
                //DO NOTHING
            }
            return Task.CompletedTask;
        }

        public void Start()
        {
            try
            {
                Logger.LogToConsole(LogType.Info, $"Starting Server - Port: {ServerProperties.Properties.MCCommPortTCP}", nameof(MCComm));
                Listener.Start();
                Listener.BeginGetContext(new AsyncCallback(Listen), null);

                Logger.LogToConsole(LogType.Info, $"Server Key: {ServerProperties.Properties.PermanentServerKey}", nameof(MCComm));
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
                    Console.WriteLine($"Missing permissions to listen on http://*:{ServerProperties.Properties.MCCommPortTCP}/\n");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Please give access by typing in the following command in a command prompt\nnetsh http add urlacl url=http://*:{ServerProperties.Properties.MCCommPortTCP}/ user={userdomain}\\{username} listen=yes\nAnd then restart the server\n");
                    Console.WriteLine("Or run this application as an Administrator.");
                    Console.ResetColor();
                }

                ServerEvents.InvokeFailed(nameof(MCComm), ex.Message);
            }
        }

        private void Listen(IAsyncResult result)
        {
            try
            {
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
                        else if (json.LoginKey != ServerProperties.Properties.PermanentServerKey)
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
                                var participant = ServerData.GetParticipantByKey(json.PlayerKey);
                                if (participant == null)
                                {
                                    SendResponse(ctx, HttpStatusCode.NotFound, "Error. Could not find key!");
                                    break;
                                }
                                if (participant.Binded)
                                {
                                    SendResponse(ctx, HttpStatusCode.Conflict, "Error. Key has already been binded to a participant.");
                                    break;
                                }
                                if (ServerData.GetParticipantByMinecraftId(json.PlayerId).Value != null)
                                {
                                    SendResponse(ctx, HttpStatusCode.Conflict, "Error. PlayerId is already binded to a participant!");
                                    break;
                                }

                                participant.MinecraftData.Gamertag = json.Gamertag;
                                participant.MinecraftData.PlayerId = json.PlayerId;
                                participant.Binded = true;
                                SendResponse(ctx, HttpStatusCode.Accepted, "Successfully Binded");
                                ServerEvents.InvokeParticipantBinded(participant, json.PlayerKey);
                                break;

                            case PacketType.Update:
                                for (int i = 0; i < json.Players.Count; i++)
                                {
                                    var player = json.Players[i];
                                    var vcParticipant = ServerData.GetParticipantByMinecraftId(player.PlayerId).Value;
                                    if (vcParticipant != null && !vcParticipant.ClientSided)
                                    {
                                        if (!vcParticipant.MinecraftData.Position.Equals(player.Location))
                                            vcParticipant.MinecraftData.Position = player.Location;

                                        if(vcParticipant.MinecraftData.DimensionId != player.DimensionId)
                                            vcParticipant.MinecraftData.DimensionId = player.DimensionId;

                                        if(vcParticipant.MinecraftData.Rotation != player.Rotation)
                                            vcParticipant.MinecraftData.Rotation = player.Rotation;

                                        if (vcParticipant.MinecraftData.CaveDensity != player.CaveDensity)
                                            vcParticipant.MinecraftData.CaveDensity = player.CaveDensity;
                                    }
                                }
                                SendResponse(ctx, HttpStatusCode.OK, "Updated");
                                break;

                            case PacketType.UpdateSettings:
                                if (json.Settings.ProximityDistance <= 0 || json.Settings.ProximityDistance > 60)
                                {
                                    SendResponse(ctx, HttpStatusCode.NotAcceptable, "Error. Proximity distance must be higher than 0 or lower than 61!");
                                    break;
                                }

                                ServerProperties.Properties.ProximityDistance = json.Settings.ProximityDistance;
                                ServerProperties.Properties.ProximityToggle = json.Settings.ProximityToggle;
                                ServerProperties.Properties.VoiceEffects = json.Settings.VoiceEffects;
                                SendResponse(ctx, HttpStatusCode.OK, "Updated Settings");
                                break;

                            case PacketType.GetSettings:
                                var settingsPacket = new WebserverPacket()
                                {
                                    Settings = new ServerSettings()
                                    {
                                        ProximityDistance = ServerProperties.Properties.ProximityDistance,
                                        ProximityToggle = ServerProperties.Properties.ProximityToggle,
                                        VoiceEffects = ServerProperties.Properties.VoiceEffects
                                    }
                                };

                                SendResponse(ctx, HttpStatusCode.OK, JsonConvert.SerializeObject(settingsPacket));
                                break;

                            case PacketType.RemoveParticipant:
                                var rPart = ServerData.GetParticipantByMinecraftId(json.PlayerId);
                                if (rPart.Value?.MinecraftData.PlayerId == json.PlayerId)
                                {
                                    ServerData.RemoveParticipant(rPart.Key, "kicked");
                                    SendResponse(ctx, HttpStatusCode.OK, "Removed");
                                    break;
                                }

                                SendResponse(ctx, HttpStatusCode.NotFound, "Could Not Find Participant");
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
            catch
            {
                //Do Nothing
            }
        }

        private static void SendResponse(HttpListenerContext ctx, HttpStatusCode code, string Content)
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
