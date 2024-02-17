using NAudio.Wave;
using System.Linq;
using VoiceCraft.Core.Client;

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

        public JitterBuffer(WaveFormat WaveFormat, int bufferMilliseconds = 80, int maxBufferSizeMilliseconds = 1000)
        {
            this.WaveFormat = WaveFormat;
            QueueLength = bufferMilliseconds / VoiceCraftClient.FrameMilliseconds;
            MaxQueueLength = maxBufferSizeMilliseconds / VoiceCraftClient.FrameMilliseconds;
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

        public StatusCode Get(ref AudioFrame outFrame)
        {
            if (!IsPreloaded)
                return StatusCode.Failed;

            RemoveOldFrames();

            var earliest = FindEarliestSlot();
            if (earliest != -1)
            {
                var distance = QueuedFrames[earliest].Sequence - Sequence;

                if (distance == 1 || IsFirst)
                {
                    //This is the frame we expected
                    Sequence = QueuedFrames[earliest].Sequence;
                    IsFirst = false;
                    SilencedFrames = 0;

                    outFrame.Buffer = QueuedFrames[earliest].Buffer;
                    outFrame.Sequence = QueuedFrames[earliest].Sequence;

                    QueuedFrames[earliest].Buffer = null;
                    return StatusCode.Success;
                }
                else if (distance == 2)
                {
                    //Missed this frame, but the next queued one might have FEC info
                    Sequence++;
                    outFrame.Buffer = QueuedFrames[earliest].Buffer;
                    outFrame.Sequence = Sequence;
                    return StatusCode.Missed;
                }
                else
                {
                    //Missed this frame and we have no FEC data to work with
                    return StatusCode.Failed;
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

            return StatusCode.Failed;
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
            int index = -1;
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

    public struct Frame
    {
        public byte[]? Buffer;
        public uint Sequence;
    }


    public struct AudioFrame
    {
        public byte[]? Buffer;
        public uint Sequence;
    }

    public enum StatusCode
    {
        Failed = -1,
        Success = 0,
        Missed = 1
    }
}
