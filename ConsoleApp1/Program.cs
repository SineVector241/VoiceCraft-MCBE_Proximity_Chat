using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceCraft_Server
{
    public class Program
    {
        public static void Main(string[] args) => new MainEntry().StartServer().GetAwaiter().GetResult();
    }
}
