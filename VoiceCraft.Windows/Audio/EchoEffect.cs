using System;
using System.Collections.Generic;
using VoiceCraft.Windows.Interfaces;

namespace VoiceCraft.Windows.Audio
{
    public class EchoEffect : IEffect
    {
        public int EchoLength { get; set; }
        public float EchoFactor { get; set; }

        private Queue<float> Samples;

        public EchoEffect(int length = 20000, float factor = 0.0f)
        {
            Samples = new Queue<float>();
            EchoLength = length;
            EchoFactor = factor;

            for(int i = 0; i < length; i++) Samples.Enqueue(0.0f);
        }


        public float ApplyEffect(float sample)
        {
            Samples.Enqueue(sample);
            if (EchoFactor != 0.0f)
                return Math.Min(1, Math.Max(-1, sample + EchoFactor * Samples.Dequeue()));
            else
                return sample;

        }
    }
}

//Credits https://www.youtube.com/watch?v=ZiIJRvNx2N0&t