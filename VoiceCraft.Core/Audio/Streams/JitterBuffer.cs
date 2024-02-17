using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Core.Audio.Streams
{
    public class JitterBuffer
    {
        public WaveFormat WaveFormat { get; set; }
        private readonly int QueueLength;
        private readonly SemaphoreSlim QueueLock;

        private uint Sequence;
        private uint Timestamp;
        private bool IsFirst;
        private long NextTick = Environment.TickCount;
        private int SilencedFrames = 0;

        private ConcurrentQueue<Frame> QueuedFrames { get; set; }

        public JitterBuffer(WaveFormat WaveFormat, int bufferMillis = 80)
        {
            this.WaveFormat = WaveFormat;
            QueueLength = (bufferMillis + (VoiceCraftClient.FrameMilliseconds - 1)) / bufferMillis;

            QueuedFrames = new ConcurrentQueue<Frame>();
            QueueLock = new SemaphoreSlim(QueueLength, QueueLength);
            IsFirst = true;
        }

        public async Task AddFrame()
        {
        }

        public int Get(ref AudioFrame outFrame)
        {
            //Don't want to give a packet as soon as it comes in.
            long tick = Environment.TickCount;
            long dist = NextTick - tick;
            if (dist > 0)
            {
                Task.Delay((int)dist).GetAwaiter().GetResult();
                return -1;
            }
            NextTick += VoiceCraftClient.FrameMilliseconds;


            if (QueuedFrames.TryPeek(out Frame frame))
            {
                uint distance = frame.Timestamp - Timestamp;
                bool restartSeq = IsFirst;

                if (!IsFirst)
                {
                    if (distance > uint.MaxValue - WaveFormat.ConvertLatencyToByteSize(5000))
                    {
                        QueuedFrames.TryDequeue(out _);
                        QueueLock.Release();
                        return -1; //Dropped the frame.
                    }
                }

                if (distance == 0 || restartSeq)
                {
                    //This is the frame we expected
                    Sequence = frame.Sequence;
                    Timestamp = frame.Timestamp;
                    IsFirst = false;
                    SilencedFrames = 0;

                    QueuedFrames.TryDequeue(out _);
                    outFrame.Buffer = frame.Buffer;
                    outFrame.Sequence = frame.Sequence;
                    QueueLock.Release();
                    return 0; //Successful Retreival, Decode Normally.
                }
                else if (distance == WaveFormat.ConvertLatencyToByteSize(VoiceCraftClient.FrameMilliseconds))
                {
                    //Missed this frame, but the next queued one might have FEC info
                    Sequence++;
                    outFrame.Buffer = frame.Buffer;
                    outFrame.Sequence = frame.Sequence;
                    outFrame.Missed = true;
                    return 1; //1 = Missed Packet but expect decode.
                }
                else
                {
                    //Missed this frame and we have no FEC data to work with
                    outFrame.Sequence = Sequence++;
                    outFrame.Missed = true;
                    return 2; //2 = Missed Packet, Decode with PLC?.
                }
            }
            else if (!IsFirst)
            {
                //Missed this frame and we have no FEC data to work with
                if (SilencedFrames < 5)
                    SilencedFrames++;
                else
                {
                    IsFirst = true;
                    //_isPreloaded = false;
                }

                return 2; //2 = Missed Packet, Decode with PLC?.
            }
            Timestamp += (uint)WaveFormat.ConvertLatencyToByteSize(VoiceCraftClient.FrameMilliseconds);
            return -1; //Failed, Do Nothing.
        }

        private struct Frame
        {
            public readonly byte[] Buffer;
            public readonly int Bytes;
            public readonly uint Sequence;
            public readonly uint Timestamp;

            public Frame(byte[] buffer, int bytes, uint sequence, uint timestamp)
            {
                Buffer = buffer;
                Bytes = bytes;
                Sequence = sequence;
                Timestamp = timestamp;
            }
        }
    }

    public struct AudioFrame
    {
        public byte[]? Buffer;
        public uint Sequence;
        public bool Missed;
    }
}
