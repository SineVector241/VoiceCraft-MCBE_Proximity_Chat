using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoiceCraft_Mobile.Audio
{
    public class AudioPlayback
    {
        private readonly IWavePlayer trackOut;
        private readonly MixingSampleProvider mixer;

        public readonly WaveFormat recordFormat = new WaveFormat(16000, 1);
        public readonly WaveFormat playFormat = WaveFormat.CreateIeeeFloatWaveFormat(16000, 1);

        public AudioPlayback()
        {
            trackOut = new AudioTrackOut() { DesiredLatency = 400, NumberOfBuffers = 3 };
            mixer = new MixingSampleProvider(playFormat) { ReadFully = true };

            trackOut.Init(mixer);
            trackOut.Play();
        }

        public void AddMixerInput(BufferedWaveProvider waveProvider)
        {
            mixer.AddMixerInput(waveProvider);
        }

        public static readonly AudioPlayback Current = new AudioPlayback();
    }
}
