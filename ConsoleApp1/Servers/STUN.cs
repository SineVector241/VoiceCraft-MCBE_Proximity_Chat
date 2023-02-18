using System;
using System.Net;
using SIPSorcery.Net;

namespace VoiceCraft_Server.Servers
{
    public class STUN
    {
        private STUNListener primarySTUNListener;
        private STUNListener secondarySTUNListener;
        private STUNServer stunServer;
        public STUN(int Port)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Starting STUN Server on ports {Port + 2}, {Port + 3}");
                Console.ResetColor();

                //Bind Addresses
                IPEndPoint primaryEndPoint = new IPEndPoint(IPAddress.Any, Port + 2);
                IPEndPoint secondaryEndPoint = new IPEndPoint(IPAddress.Any, Port + 3);

                //Intialise new stun listener and server
                primarySTUNListener = new STUNListener(primaryEndPoint);
                secondarySTUNListener = new STUNListener(secondaryEndPoint);
                stunServer = new STUNServer(primaryEndPoint, primarySTUNListener.Send, secondaryEndPoint, secondarySTUNListener.Send);

                //Hook Events
                primarySTUNListener.MessageReceived += stunServer.STUNPrimaryReceived;
                secondarySTUNListener.MessageReceived += stunServer.STUNSecondaryReceived;
                Events.OnExit += Shutdown;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("STUN server successfully initialised.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"STUN server failed to initialised. Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        private void Shutdown(object sender, EventArgs e)
        {
            primarySTUNListener.Close();
            secondarySTUNListener.Close();
            stunServer.Stop();
        }
    }
}
