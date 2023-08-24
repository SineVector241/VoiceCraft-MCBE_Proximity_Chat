using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace VoiceCraft.Mobile.Audio
{
    public class EchoSampleProvider : ISampleProvider
    {
        private ISampleProvider source;
        public int EchoLength { get; private set; }
        public float EchoFactor { get; set; }

        private Queue<float> Samples;

        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public EchoSampleProvider(ISampleProvider source, int echoLength = 10000)
        {
            this.source = source;
            Samples = new Queue<float>();
            EchoLength = echoLength;
            EchoFactor = 0.0f;

            for (int i = 0; i < echoLength; i++) Samples.Enqueue(0.0f);
        }


        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            for (int i = 0; i < read; i++)
            {
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    float sample = buffer[offset + i];
                    Samples.Enqueue(sample);
                    buffer[offset + i] = Math.Min(1, Math.Max(-1, sample + EchoFactor * Samples.Dequeue()));
                }
            }

            return read;
        }
    }
}

//Credits https://www.youtube.com/watch?v=ZiIJRvNx2N0&t