using Newtonsoft.Json;
using VoiceCraft.Core.Packets.MCWSS;

namespace VoiceCraft.Core.Packets
{
    public class MCWSSPacket<T> where T : new()
    {
        public Header header { get; set; }
        public T body { get; set; }

        public MCWSSPacket()
        {
            header = new Header();
            body = new T();
        }

        public string SerializePacket()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
