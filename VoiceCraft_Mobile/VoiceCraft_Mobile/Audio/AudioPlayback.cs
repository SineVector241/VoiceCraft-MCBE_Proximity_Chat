using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace VoiceCraft_Mobile.Audio
{
    public class AudioPlayback
    {
        private AudioTrackOut trackOut;
        private MixingSampleProvider mixer;

        public readonly WaveFormat audioFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);
        public readonly WaveFormat recordFormat = new WaveFormat(44100, 1);

        public AudioPlayback()
        {
            try
            {
                trackOut = new AudioTrackOut();

                mixer = new MixingSampleProvider(audioFormat);
                //var signal = new SignalGenerator(audioFormat.SampleRate, 1) { Frequency = 500, Gain = 0.2, Type = SignalGeneratorType.Sin }.Take(TimeSpan.FromSeconds(20)); For Testing...
                int bufferSize = 50 * audioFormat.AverageBytesPerSecond / 1000;
                if (bufferSize % audioFormat.BlockAlign != 0)
                {
                    bufferSize -= bufferSize % audioFormat.BlockAlign;
                }
                mixer.ReadFully = true;
                trackOut.Init(mixer);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void AddMixerInput(ISampleProvider sampleProvider)
        {
            mixer.AddMixerInput(sampleProvider);
        }

        public void RemoveMixerInput(ISampleProvider provider)
        {
            mixer.RemoveMixerInput(provider);
        }

        public void Start()
        {
            try
            {
                trackOut.Play();
            }
            catch (Exception ex) { 
                Console.WriteLine(ex);
            }
        }

        public void Stop()
        {
            trackOut.Stop();
        }

        public void ClearPlayer()
        {
            mixer.RemoveAllMixerInputs();
        }

        public static readonly AudioPlayback Current = new AudioPlayback();
    }
}
