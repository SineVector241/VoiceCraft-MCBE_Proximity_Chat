using Newtonsoft.Json;
using System.Net;
using System.Numerics;
using System.Text;

namespace VoiceCraftProximityChat_Server.Servers
{
    public class WebServer
    {
        private HttpListener listener;
        private string SessionKey;
        private Server UdpServer;
        public WebServer(int Port, Server udpServer)
        {
            SessionKey = Guid.NewGuid().ToString();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{Port}/");
            try
            {
                listener.Start();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Started HttpServer: Addr - {listener.Prefixes.First()} | * Means any local IP Address E.g. the local machines IP Endpoint");
                Console.WriteLine($"Server Login/Session key is: {SessionKey}");
                Console.ResetColor();

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
                    Console.WriteLine($"Missing permissions to listen on http://*:{Port}/\n");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Please give access by typing in the following command in a command prompt\nnetsh http add urlacl url=http://*:{Port}/ user={userdomain}\\{username} listen=yes\nAnd then restart the server\n");
                    Console.WriteLine("Or run this application as an Administrator.");
                    Console.ResetColor();
                }
                else
                {
                    throw;
                }
            }
            UdpServer = udpServer;
        }
        public void listen(IAsyncResult result)
        {
            var ctx = listener.EndGetContext(result);
            listener.BeginGetContext(new AsyncCallback(listen), null);
            if (ctx.Request.HttpMethod == "POST")
            {
                try
                {
                    var content = new StreamReader(ctx.Request.InputStream).ReadToEnd();
                    var json = JsonConvert.DeserializeObject<WebserverPacket>(content);
                    if (json != null)
                    {
                        if(json.Key != SessionKey)
                        {
                            SendResponse(ctx, HttpStatusCode.Forbidden, "Invalid Key");
                            return;
                        }
                        switch (json.Type)
                        {
                            case PacketType.CreateSessionKey:
                                var login = UdpServer.CreateSessionKey(json.PlayerId);
                                if (login == null)
                                {
                                    SendResponse(ctx, HttpStatusCode.Conflict, "Player already logged in/requested");
                                }
                                else
                                {
                                    SendResponse(ctx, HttpStatusCode.OK, login);
                                }
                                break;

                            case PacketType.Update:
                                UdpServer.UpdateClientList(json.Players);
                                SendResponse(ctx, HttpStatusCode.Accepted, "OK");
                                break;

                            case PacketType.Login:
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"[HTTP] Recieved Login Request: Key: {json.Key}");
                                Console.ResetColor();
                                SendResponse(ctx, HttpStatusCode.Accepted, "OK");
                                break;
                        }
                    }
                    else
                    {
                        SendResponse(ctx, HttpStatusCode.BadRequest, "Invalid Content");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    SendResponse(ctx, HttpStatusCode.BadRequest, "Invalid Content");
                }
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
        public string Key { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public List<Player> Players { get; set; } = new List<Player>();
    }

    public class Player
    {
        public string PlayerId { get; set; } = "";
        public Vector3 Location { get; set; } = new Vector3();
    }

    public enum PacketType
    {
        CreateSessionKey,
        Update,
        Login
    }
}
