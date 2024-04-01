using System.Collections.Concurrent;
using System.Net;
using VoiceCraft.Core;
using VoiceCraft.Network.Packets;

namespace VoiceCraft.Network
{
    public class NetPeer : Disposable
    {
        private uint Sequence { get; set; } //The peer's current sequence number.
        private uint NextSequence { get; set; } //The peer's next expected receive sequence number.

        /// <summary>
        /// Endpoint of the NetPeer
        /// </summary>
        public EndPoint EP { get; set; }

        /// <summary>
        /// The cancellation token used to stop listening on the socket for this peer.
        /// </summary>
        public CancellationTokenSource CTS { get; } = new CancellationTokenSource();

        /// <summary>
        /// When the client was last active.
        /// </summary>
        public long LastActive { get; set; }

        /// <summary>
        /// The ID of the NetPeer, Used to update the endpoint if invalid.
        /// </summary>
        public long ID { get; } //Not secure enough but it'll do.

        /// <summary>
        /// The key for the NetPeer, Used as a public shareable Id.
        /// </summary>
        public ushort Key { get; set; }

        /// <summary>
        /// Send Queue.
        /// </summary>
        public ConcurrentQueue<VoiceCraftPacket> SendQueue { get; set; }

        /// <summary>
        /// Reliability Send Queue.
        /// </summary>
        public ConcurrentBag<VoiceCraftPacket> ReliabilityQueue { get; set; }

        public NetPeer(EndPoint ep, long Id, ushort key)
        {
            EP = ep;
            ID = Id;
            Key = key;
            SendQueue = new ConcurrentQueue<VoiceCraftPacket>();
            ReliabilityQueue = new ConcurrentBag<VoiceCraftPacket>();
        }

        public uint GetNextSequence()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(NetPeer));

            return Sequence++;
        }

        public static long GenerateId()
        {
            return Random.Shared.NextInt64(long.MaxValue, long.MinValue); //long.MinValue is used to specify no Id. Used when sending from server > client.
        }

        protected override void Dispose(bool disposing)
        {
            if (CTS.IsCancellationRequested)
                CTS.Dispose();
            SendQueue.Clear();
            ReliabilityQueue.Clear();
        }
    }
}
