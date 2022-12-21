using VoiceCraftProximityChat_Server.Dependencies;
using VoiceCraftProximityChat_Server.Servers;

namespace VoiceCraftProximityChat_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            //Print VoiceCraft Text
            CommandSystem commandSystem = new CommandSystem();
            string Version = "v1.2.0-alpha";
            Console.Title = $"VoiceCraft - {Version}: Idle";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("---VoiceCraft---");
            Console.WriteLine($"├ {Version} ┤\n");
            Console.ResetColor();

            int Port = 0;
            while (true)
            {
                Console.Write("Set Port: ");
                var res = int.TryParse(Console.ReadLine(), out Port);
                if(res == false) Console.WriteLine("Error. Invalid port number. Try Again");
                else break;
            }
            new Thread(() => { new UdpServer(Port); }) { IsBackground = true }.Start();
            new Thread(() => { new WebServer(Port); }) { IsBackground = true }.Start();
            Console.Title = $"VoiceCraft - {Version}: Running";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server Started. type Exit to shutdown the application...");
            Console.ResetColor();
            while(true)
            {
                var cmd = Console.ReadLine();
                if (cmd != null) commandSystem.HandleCommand(cmd);
            }
        }
    }

    public class CommandSystem
    {
        public void HandleCommand(string command)
        {
            var cmd = command.ToLower();
            switch(cmd)
            {
                case "help":
                    Console.WriteLine("Clients - Prints out the list of clients.");
                    Console.WriteLine("Keys - Prints out the list of session keys.");
                    Console.WriteLine("Exit - Shuts down the server and exits.");
                    break;

                case "clients":
                    var clients = ServerData.Data.ClientList;
                    Console.WriteLine("--- Start ---\n");
                    foreach (var client in clients)
                        Console.WriteLine($"Username: {client.Username} | Key: {client.Key} | PlayerId: {client.PlayerId} | Ready: {client.isReady} | EnviromentId: {client.EnviromentId} | Location: {client.Location}");
                    Console.WriteLine("\n---- End ----");
                    Console.WriteLine($"Online Clients: {clients.Count}");
                    break;

                case "keys":
                    var keys = ServerData.Data.SessionKeys;
                    Console.WriteLine("--- Start ---\n");
                    foreach (var key in keys)
                        Console.WriteLine($"Username: {key.Username} | PlayerId {key.PlayerId} | Key: {key.Key} | Registered-At: {key.RegisteredAt}");
                    Console.WriteLine("\n---- End ----");
                    Console.WriteLine($"Session Key Count: {keys.Count}");
                    break;

                case "exit":
                    Console.WriteLine("Shutting down server...");
                    Environment.Exit(0);
                    break;
            }
        }
    }
}