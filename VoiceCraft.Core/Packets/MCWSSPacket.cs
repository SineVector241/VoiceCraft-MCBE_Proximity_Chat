using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VoiceCraft.Core.Packets.MCWSS;

namespace VoiceCraft.Core.Packets
{
    public class MCWSSPacket
    {
        public Header Header { get; set; }
        public object Body { get; set; }

        public MCWSSPacket()
        {
            Header = new Header();
            Body = new object();
        }

        public MCWSSPacket(string data)
        {
            Body = new object();
            Header = new Header();

            var jsonData = JObject.Parse(data);
            if(jsonData != null)
            {
                var header = jsonData["header"]?.ToObject<Header>();
                if (header == null)
                    throw new JsonReaderException("Invalid Content!");

                Header = header;
                if(Header.messagePurpose == "event")
                {
                    switch(Header.eventName)
                    {
                        case "PlayerTravelled":
                            var body = jsonData["body"]?.ToObject<PlayerTravelledEvent>();
                            if (body == null) 
                                throw new JsonReaderException("Invalid Data!");
                            Body = body;
                            break;
                    }
                }
                else if(Header.messagePurpose == "commandResponse")
                {
                    //Honestly, We only need this response.
                    var body = jsonData["body"]?.ToObject<LocalPlayerNameResponse>();
                    if (body == null)
                        throw new JsonReaderException("Invalid Data!");
                    Body = body;
                }
            }
        }
    }
}
