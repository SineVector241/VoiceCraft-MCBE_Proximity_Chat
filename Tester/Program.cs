using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Web;

namespace Tester
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new TcpWebServer(9050);
        }
    }
    public class TcpWebServer
    {
        private HttpListener listener;
        private byte[] dataStream = new byte[1024];
        private bool isConnected = false;
        public TcpWebServer(int Port)
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{Port}/");
            try
            {
                listener.Start();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Started HttpServer - Bound to address: {listener.Prefixes.First()}");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("* Means that it is listening both local and remote");
                Console.ResetColor();
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
                    Console.WriteLine($"Please give access by typing in the following command in a command prompt\nnetsh http add urlacl url=http://*:{Port}/ user={userdomain}\\{username} listen=yes\n");
                    Console.WriteLine("Or run this application as Administrator.");

                    Console.ResetColor();
                    Console.WriteLine("\nPress any key to close the application...");
                }
            }
        }
    }
}