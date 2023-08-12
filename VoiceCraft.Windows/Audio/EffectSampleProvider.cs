using NAudio.Wave;
using System.Collections.Generic;
using VoiceCraft.Windows.Interfaces;

namespace VoiceCraft.Windows.Audio
{
    public class EffectsSampleProvider : ISampleProvider
    {
        private ISampleProvider source;

        public List<IEffect> Effects { get; private set; }

        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public EffectsSampleProvider(ISampleProvider source)
        {
            this.source = source;
            Effects = new List<IEffect>();
        }

        private int channel = 0;

        public int Read(float[] buffer, int offset, int count)
        {
            int read = source.Read(buffer, offset, count);
            for (int i = 0; i < read; i++)
            {
                float sample = buffer[offset + i];
                if (Effects.Count == WaveFormat.Channels)
                {
                    sample = Effects[channel].ApplyEffect(sample);
                    channel = (channel + 1) % WaveFormat.Channels;
                }

                buffer[offset + i] = sample;
            }

            return read;
        }
    }
}

//Credits https://www.youtube.com/watch?v=ZiIJRvNx2N0&t
