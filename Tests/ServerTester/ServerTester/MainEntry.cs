using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.MCComm;

namespace ServerTester
{
    public class MainEntry
    {
        public HttpClient HttpClient { get; set; }
        public MainEntry()
        {
            HttpClient = new HttpClient();

            var packet = new MCCommPacket()
            {
                PacketType = MCCommPacketTypes.Login,
                PacketData = new Login()
                {
                    LoginKey = "test"
                }
            };

            using StringContent content = new StringContent(packet.GetPacketString());
            Task.Run(async () =>
            {
                var response = await HttpClient.PostAsync("http://127.0.0.1:9050/", content);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine(jsonResponse);
            });

            Console.ReadLine();
        }
    }
}
