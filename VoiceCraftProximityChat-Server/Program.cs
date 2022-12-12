using VoiceCraftProximityChat_Server.Servers;

namespace VoiceCraftProximityChat_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            int Port = 0;
            while (true)
            {
                try
                {
                    Console.Write("Set Port: ");
                    Port = Convert.ToInt16(Console.ReadLine());
                    break;
                }
                catch
                {
                    Console.WriteLine("Error. Invalid port number. Try Again");
                }
            }
            new Thread(() => { new UdpServer(Port); }) { IsBackground = true}.Start();
            new Thread(() => { new WebServer(Port); }) { IsBackground = true }.Start();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Server Started. Press any key to shutdown...");
            Console.ResetColor();
            Console.ReadKey();
        }
    }
}