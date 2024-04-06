using System.Collections.Concurrent;
using System.Net;
using VoiceCraft.Core.Packets;
using VoiceCraft.Core.Packets.VoiceCraft;

namespace VoiceCraft.Network
{
    public class NetPeer
    {
        public const int ResendTime = 300;
        public const int RetryResendTime = 500;
        public const int MaxSendRetries = 20;
        public const int MaxRecvBufferSize = 30; //30 packets.

        public delegate void PacketReceived(NetPeer peer, VoiceCraftPacket packet);
        public event PacketReceived? OnPacketReceived;
        private uint Sequence;
        private uint NextSequence;
        private ConcurrentDictionary<uint, VoiceCraftPacket> ReliabilityQueue { get; set; }
        private ConcurrentDictionary<uint, VoiceCraftPacket> ReceiveBuffer { get; set; }

        /// <summary>
        /// Defines wether the client was denied.
        /// </summary>
        public bool Denied { get; private set; }

        /// <summary>
        /// Defines wether the client is sucessfully connected and accepted.
        /// </summary>
        public bool Connected { get; set; }

        /// <summary>
        /// Endpoint of the NetPeer.
        /// </summary>
        public EndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// When the client was last active.
        /// </summary>
        public long LastActive { get; set; } = Environment.TickCount64;

        /// <summary>
        /// The ID of the NetPeer, Used to update the endpoint if invalid.
        /// </summary>
        public long Id { get; set; } //Not secure enough but it'll do.

        /// <summary>
        /// Send Queue.
        /// </summary>
        public ConcurrentQueue<VoiceCraftPacket> SendQueue { get; set; }

        public NetPeer(EndPoint ep, long Id)
        {
            RemoteEndPoint = ep;
            this.Id = Id;
            SendQueue = new ConcurrentQueue<VoiceCraftPacket>();
            ReliabilityQueue = new ConcurrentDictionary<uint, VoiceCraftPacket>();
            ReceiveBuffer = new ConcurrentDictionary<uint, VoiceCraftPacket>();
        }

        public void AddToSendBuffer(VoiceCraftPacket packet)
        {
            if (packet.IsReliable)
            {
                packet.Sequence = Sequence;
                packet.ResendTime = Environment.TickCount64 + ResendTime;
                ReliabilityQueue.TryAdd(packet.Sequence, packet); //If reliable, Add to reliability queue. ResendTime is determined by the application.
                Sequence++;
            }

            packet.Id = Id;
            SendQueue.Enqueue(packet);
        }

        public bool AddToReceiveBuffer(VoiceCraftPacket packet)
        {
            LastActive = Environment.TickCount64;

            if (Connected && packet.Id != Id) return false; //Invalid Id.

            if(ReceiveBuffer.Count >= MaxRecvBufferSize && packet.Sequence != NextSequence)
                return false; //make sure it doesn't overload the receive buffer and cause a memory overflow.

            if(!packet.IsReliable)
            {
                OnPacketReceived?.Invoke(this, packet);
                return true; //Not reliable, We can just say it's received.
            }

            AddToSendBuffer(new Ack() { PacketSequence = packet.Sequence }); //Acknowledge packet by sending the Ack packet.
            if (packet.Sequence < NextSequence) return true; //Likely to be a duplicate packet.

            ReceiveBuffer.TryAdd(packet.Sequence, packet); //Add it in, TryAdd does not replace an old packet.
            foreach (var p in ReceiveBuffer)
            {
                if (p.Key == NextSequence && ReceiveBuffer.TryRemove(p)) //Remove packet and notify listeners.
                {
                    NextSequence++; //Update next expected packet.
                    OnPacketReceived?.Invoke(this, p.Value);
                }
            }
            return true;
        }

        public void ResendPackets()
        {
            foreach (var packet in ReliabilityQueue)
            {
                if (packet.Value.ResendTime <= Environment.TickCount64)
                {
                    packet.Value.ResendTime = Environment.TickCount64 + RetryResendTime; //More delay.
                    packet.Value.Retries++;
                    SendQueue.Enqueue(packet.Value);
                }
            }
        }

        public void AcceptLogin(short key)
        {
            if (!Connected)
            {
                AddToSendBuffer(new Accept() { Key = key });
                Connected = true;
            }
        }

        public void DenyLogin(string? reason = null)
        {
            if (!Connected)
            {
                AddToSendBuffer(new Deny() { Reason = reason ?? string.Empty });
                Denied = true;
            }
        }

        public void AcknowledgePacket(uint packetId)
        {
            ReliabilityQueue.TryRemove(packetId, out var _);
        }

        public static long GenerateId()
        {
            return Random.Shared.NextInt64(long.MinValue + 1, long.MaxValue); //long.MinValue is used to specify no Id.
        }

        public void Reset()
        {
            SendQueue.Clear();
            ReliabilityQueue.Clear();
            ReceiveBuffer.Clear();
            NextSequence = 0;
            Sequence = 0;
        }
    }
}
