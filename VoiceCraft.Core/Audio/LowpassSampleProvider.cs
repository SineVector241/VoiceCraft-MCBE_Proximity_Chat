using NAudio.Dsp;
using NAudio.Wave;

namespace VoiceCraft.Core.Audio
{
    public class LowpassSampleProvider : ISampleProvider
    {
        private ISampleProvider source;
        private BiQuadFilter filter;
        public bool Enabled { get; set; } = true;
        public LowpassSampleProvider(ISampleProvider source, int cutOffFreq, int bandWidth)
        {
            this.source = source;

            filter = BiQuadFilter.LowPassFilter(source.WaveFormat.SampleRate, cutOffFreq, bandWidth);
        }
        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = source.Read(buffer, offset, count);

            if (Enabled)
            {
                for (int i = 0; i < samplesRead; i++)
                    buffer[offset + i] = filter.Transform(buffer[offset + i]);
            }

            return samplesRead;
        }
    }
}
