using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using VoiceCraft.Core.Packets.MCWSS;

namespace VoiceCraft.Core.Packets
{
    public class MCPacketRegistry
    {
        private ConcurrentDictionary<Header, Type> RegisteredPackets = new ConcurrentDictionary<Header, Type>();

        /// <summary>
        /// Registers a packet.
        /// </summary>
        /// <param name="Id">The Id of the packet.</param>
        /// <param name="Type">The type to create for the data to be parsed.</param>
        /// <param name="IsReliable"></param>
        public void RegisterPacket(Header header, Type PacketType)
        {
            RegisteredPackets.AddOrUpdate(header, PacketType, (key, old) => old = PacketType);
        }

        /// <summary>
        /// Deregisters a packet.
        /// </summary>
        /// <param name="Id">The Id of the packet.</param>
        /// <returns>The deregistered packet type.</returns>
        public Type? DeregisterPacket(Header header)
        {
            if (RegisteredPackets.TryRemove(header, out var packet)) return packet;
            return null;
        }

        /// <summary>
        /// Deregisters all registered packets.
        /// </summary>
        public void DeregisterAll()
        {
            RegisteredPackets.Clear();
        }

        /// <summary>
        /// Converts a packet from a json string to the object.
        /// </summary>
        /// <param name="data">The raw data.</param>
        /// <returns>The packet.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public object GetPacketFromJsonString(string data)
        {
            var header = JObject.Parse(data)["header"]?.ToObject<Header>() ?? new Header(); //Packet Header.

            if (!RegisteredPackets.TryGetValue(header, out var packetType))
                throw new InvalidOperationException($"Invalid packet header {header}");

            var packet = GetMCWSSPacketFromType(data, packetType);
            return packet;
        }

        /// <summary>
        /// Create's a packet from the MCWSS packet body.
        /// </summary>
        /// <param name="PacketType">The packet type.</param>
        /// <returns>The packet</returns>
        /// <exception cref="Exception"></exception>
        public static object GetMCWSSPacketFromType(string data, Type PacketType)
        {
            var packet = JsonConvert.DeserializeObject(data, PacketType);
            if (packet == null) throw new Exception("Could not create packet instance.");

            return packet;
        }
    }
}
