using NAudio.Wave;

namespace VoiceCraft.Core.Audio
{
    public class SmoothVolumeSampleProvider : ISampleProvider
    {
        private ISampleProvider source;
        private float fadeSamplePosition;
        private int fadeSamples;
        private float fadeDurationMS;
        private float targetVolume;
        private float previousVolume;
        public float TargetVolume
        {
            get
            {
                return targetVolume;
            }
            set
            {
                if(targetVolume == value) return; //Return since it's the same target volume, and we don't want to reset the counter.
                previousVolume = targetVolume;
                targetVolume = value;
                fadeSamplePosition = 0.0f; //Reset position since we have a new target.
            }
        }
        public float FadeDurationMS { 
            get 
            { 
                return fadeDurationMS; 
            } 
            set 
            { 
                fadeDurationMS = value;
                var newSamples = (int)(FadeDurationMS * source.WaveFormat.SampleRate / 1000);
                if(newSamples < fadeSamplePosition) //Make sure we don't overshoot the target when lerping.
                {
                    fadeSamplePosition = newSamples;
                }
                fadeSamples = newSamples;
            } 
        }
        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public SmoothVolumeSampleProvider(ISampleProvider source, float fadeDurationMS)
        {
            this.source = source;
            fadeSamplePosition = 0.0f;
            FadeDurationMS = fadeDurationMS;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = source.Read(buffer, offset, count);
            int sample = 0;
            while (sample < read)
            {
                var value = Lerp(previousVolume, TargetVolume, fadeSamplePosition / fadeSamples);
                for (int ch = 0; ch < source.WaveFormat.Channels; ch++)
                {
                    buffer[offset + sample++] *= value;
                }
                //Make sure we set all others ahead to the same volume value and not overshoot the target...
                if (fadeSamplePosition <= fadeSamples)
                {
                    fadeSamplePosition++;
                }
            }
            return read;
        }

        private float Lerp(float current, float target, float by)
        {
            return current * (1 - by) + target * by;
        }
    }
}
