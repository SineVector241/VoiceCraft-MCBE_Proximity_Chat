using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceCraft_Server.Servers;

namespace VoiceCraft_Server
{
    public class MainEntry
    {
        public MainEntry()
        {
            new STUN(9050);
            Console.ReadKey();
        }

        public async Task StartServer()
        {

        }
    }
}
