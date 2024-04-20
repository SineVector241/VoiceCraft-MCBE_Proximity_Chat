using NAudio.Wave;
using OpusSharp.Core;
using System;
using System.Linq;

namespace VoiceCraft.Core.Audio.Streams
{
    public class JitterBuffer
    {
        public WaveFormat WaveFormat { get; set; }
        public int Count => QueuedFrames.Count(x => x.Buffer != null);
        private bool IsFirst;
        private bool IsPreloaded;
        private Frame[] QueuedFrames;
        private int QueueLength;
        private int MaxQueueLength;
        private uint Sequence;

        private int SilencedFrames;

        public JitterBuffer(WaveFormat WaveFormat, int bufferMilliseconds = 80, int maxBufferSizeMilliseconds = 1000, int frameSizeMS = 20)
        {
            this.WaveFormat = WaveFormat;
            QueueLength = bufferMilliseconds / frameSizeMS;
            MaxQueueLength = maxBufferSizeMilliseconds / frameSizeMS;
            QueuedFrames = new Frame[MaxQueueLength];
            IsFirst = true;
        }

        public void Put(Frame frame)
        {
            if (IsFirst)
            {
                QueuedFrames[0] = frame;
                Sequence = frame.Sequence;
                SilencedFrames = 0;
                IsFirst = false;
                return;
            }
            //Remove Old Frames.
            RemoveOldFrames();

            //Insert the packet if it's not lower than the current sequence.
            if (frame.Sequence > Sequence)
            {
                //Find an empty slot.
                var empty = FindEmptySlot();
                if (empty != -1)
                {
                    QueuedFrames[empty] = frame;
                }
            }
            else
            {
                //We haven't found an empty slot so we replace the oldest/highest packet sequence.
                var oldest = FindOldestSlot();
                QueuedFrames[oldest] = frame;
            }

            if(!IsPreloaded && Count >= QueueLength)
            {
                IsPreloaded = true;
            }
        }

        public StatusCode Get(ref Frame outFrame)
        {
            var status = StatusCode.Failed;
            if (!IsPreloaded)
                return StatusCode.NotReady;

            RemoveOldFrames();

            var earliest = FindEarliestSlot();
            if (earliest != -1)
            {
                uint earliestSequence = QueuedFrames[earliest].Sequence;
                uint currentSequence = Sequence;
                var distance = earliestSequence - currentSequence;


                if (distance == 0 || IsFirst)
                {
                    //This is the frame we expected
                    Sequence = QueuedFrames[earliest].Sequence;
                    IsFirst = false;
                    SilencedFrames = 0;

                    outFrame.Buffer = QueuedFrames[earliest].Buffer;
                    outFrame.Length = QueuedFrames[earliest].Length;
                    outFrame.Sequence = QueuedFrames[earliest].Sequence;

                    QueuedFrames[earliest].Buffer = null;
                    status = StatusCode.Success;
                }
                else if (distance == 1)
                {
                    //Missed this frame, but the next queued one might have FEC info
                    Sequence++;
                    outFrame.Buffer = QueuedFrames[earliest].Buffer;
                    outFrame.Length = QueuedFrames[earliest].Length;
                    outFrame.Sequence = Sequence;
                    status = StatusCode.Missed;
                }
                else
                {
                    //Missed this frame and we have no FEC data to work with
                    Sequence++;
                    status = StatusCode.Failed;
                }
            }
            else if (!IsFirst)
            {
                if (SilencedFrames < 5)
                    SilencedFrames++;
                else
                {
                    IsFirst = true;
                    IsPreloaded = false;
                }
            }
            Sequence++;
            return status;
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < QueuedFrames.Length; i++)
            {
                if (QueuedFrames[i].Buffer == null)
                {
                    return i;
                }
            }
            return -1;
        }

        private int FindOldestSlot()
        {
            int index = 0;
            uint oldest = QueuedFrames[index].Sequence;
            for (int i = 1; i < QueuedFrames.Length; i++)
            {
                if (QueuedFrames[i].Sequence > oldest)
                {
                    oldest = QueuedFrames[i].Sequence;
                    index = i;
                }
            }
            return index;
        }

        private int FindEarliestSlot()
        {
            uint earliest = QueuedFrames[0].Sequence;
            int index = QueuedFrames[0].Buffer != null ? 0 : -1;
            for (int i = 1; i < QueuedFrames.Length; i++)
            {
                if (QueuedFrames[i].Buffer != null && QueuedFrames[i].Sequence < earliest)
                {
                    earliest = QueuedFrames[i].Sequence;
                    index = i;
                }
            }

            return index;
        }

        private void RemoveOldFrames()
        {
            //Remove Old Packets
            for (int i = 0; i < QueuedFrames.Length; i++)
            {
                if (QueuedFrames[i].Buffer != null && QueuedFrames[i].Sequence < Sequence)
                {
                    QueuedFrames[i].Buffer = null;
                }
            }
        }
    }

    public class VoiceCraftJitterBuffer
    {
        public readonly int FrameSizeMS;
        private Frame inFrame;
        private Frame outFrame;
        private readonly JitterBuffer JitterBuffer;
        private readonly OpusDecoder Decoder;

        public VoiceCraftJitterBuffer(WaveFormat waveFormat, int frameSizeMS = 20)
        {
            FrameSizeMS = frameSizeMS;
            JitterBuffer = new JitterBuffer(waveFormat, 80, 2000, frameSizeMS);
            Decoder = new OpusDecoder(waveFormat.SampleRate, waveFormat.Channels);
        }

        public int Get(byte[] decodedFrame)
        {
            if (outFrame.Buffer == null)
            {
                outFrame.Buffer = new byte[decodedFrame.Length];
            }
            else
                Array.Clear(outFrame.Buffer, 0, outFrame.Buffer.Length);

            var bytesRead = 0;
            lock (JitterBuffer)
            {
                var status = JitterBuffer.Get(ref outFrame);
                if (status == StatusCode.Success)
                {
                    bytesRead = Decoder.Decode(outFrame.Buffer, outFrame.Length, decodedFrame, decodedFrame.Length);
                }
                else if(status == StatusCode.NotReady)
                {
                    return 0;
                }
                else
                {
                    bytesRead = Decoder.Decode(null, 0, decodedFrame, decodedFrame.Length);
                }
            }

            return bytesRead;
        }

        public void Put(byte[] data, uint packetCount)
        {
            inFrame.Buffer = data;
            inFrame.Length = data.Length;
            inFrame.Sequence = packetCount;
            JitterBuffer.Put(inFrame);
        }
    }

    public struct Frame
    {
        public byte[]? Buffer;
        public int Length;
        public uint Sequence;
    }

    public enum StatusCode
    {
        NotReady = -2,
        Failed = -1,
        Success = 0,
        Missed = 1
    }
}