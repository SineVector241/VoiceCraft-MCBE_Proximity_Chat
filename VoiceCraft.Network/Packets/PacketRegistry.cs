using System.Collections.Concurrent;

namespace VoiceCraft.Network.Packets
{
    public static class PacketRegistry
    {
        private static ConcurrentDictionary<byte, Type> RegisteredPackets = new ConcurrentDictionary<byte, Type>();

        /// <summary>
        /// Registers a packet.
        /// </summary>
        /// <param name="Id">The Id of the packet.</param>
        /// <param name="Type">The type to create for the data to be parsed.</param>
        /// <param name="IsReliable"></param>
        public static void RegisterPacket(byte Id, Type PacketType)
        {
            if (!typeof(VoiceCraftPacket).IsAssignableFrom(PacketType)) 
                throw new ArgumentException($"PacketType needs to inherit from {nameof(VoiceCraftPacket)}", nameof(PacketType));

            RegisteredPackets.AddOrUpdate(Id, PacketType, (key, old) => old = PacketType);
        }

        /// <summary>
        /// Deregisters a packet.
        /// </summary>
        /// <param name="Id">The Id of the packet.</param>
        /// <returns>The deregistered packet type.</returns>
        public static Type? DeregisterPacket(byte Id)
        {
            if (RegisteredPackets.TryRemove(Id, out var packet)) return packet;
            return null;
        }

        /// <summary>
        /// Deregisters all registered packets.
        /// </summary>
        public static void DeregisterAll()
        {
            RegisteredPackets.Clear();
        }

        /// <summary>
        /// Create's a packet from the type.
        /// </summary>
        /// <param name="PacketType">The packet type.</param>
        /// <returns>The packet</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public static VoiceCraftPacket GetPacketFromType(Type PacketType)
        {
            if (!typeof(VoiceCraftPacket).IsAssignableFrom(PacketType))
                throw new ArgumentException($"PacketType needs to inherit from {nameof(VoiceCraftPacket)}", nameof(PacketType));

            var packet = Activator.CreateInstance(PacketType);
            if (packet == null) throw new Exception("Could not create packet instance.");

            return (VoiceCraftPacket)packet;
        }

        /// <summary>
        /// Converts a packet from a byte array to the object.
        /// </summary>
        /// <param name="dataStream">The raw data.</param>
        /// <returns>The packet.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static VoiceCraftPacket GetPacketFromDataStream(byte[] dataStream)
        {
            var PacketId = dataStream[0]; //This is the ID.
            
            if(!RegisteredPackets.TryGetValue(PacketId, out var packetType))
                throw new InvalidOperationException($"Invalid packet id {PacketId}");

            VoiceCraftPacket packet = GetPacketFromType(packetType);
            packet.ReadPacket(ref dataStream, 1); //Offset by 1 byte so we completely remove reading the Id.

            return packet;
        }
    }
}
