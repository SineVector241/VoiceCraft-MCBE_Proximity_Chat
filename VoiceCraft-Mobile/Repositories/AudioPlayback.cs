using VoiceCraft_Mobile.Utils;
using Android.Media;
using VoiceCraft_Mobile.Network;

namespace VoiceCraft_Mobile.Repositories
{
    class AudioPlayback
    {
        public static float volumeGain { get; set; } = 1.0f;
        public AudioMixer audioMixer { get; set; }
        public AudioPlayer audioPlayer { get; set; }

        public AudioPlayback()
        {
            audioMixer = new AudioMixer();
            audioPlayer = new AudioPlayer(audioMixer);

            audioPlayer.Play();
        }


        public static readonly AudioPlayback Instance = new AudioPlayback();
    }

    public class AudioPlayer : IDisposable
    {
        public AudioTrack track { get; private set; }
        private AudioMixer audioMixer { get; }

        public AudioPlayer(AudioMixer audioMixer)
        {
            int minBufferSize = AudioTrack.GetMinBufferSize(G722ChatCodec.CodecInstance.RecordFormat.SampleRate, ChannelOut.Mono, Encoding.Pcm16bit);
            track = new AudioTrack(new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Media)
                .SetContentType(AudioContentType.Music).Build(),
                new AudioFormat.Builder()
                .SetChannelMask(ChannelOut.Mono)
                .SetEncoding(Encoding.Pcm16bit)
                .SetSampleRate(G722ChatCodec.CodecInstance.RecordFormat.SampleRate).Build(),
                minBufferSize,
                AudioTrackMode.Stream,
                AudioManager.AudioSessionIdGenerate);

            this.audioMixer = audioMixer;
        }

        public void Play()
        {
            track.SetVolume(1.0f);
            track.Play();

            ThreadPool.QueueUserWorkItem(state => StartPlayback(), null);
        }

        public void Stop()
        {
            track.Stop();
        }

        public void Dispose()
        {
            if (track.PlayState == PlayState.Playing) track.Stop();
            track.Release();
            track.Dispose();
        }

        private void StartPlayback()
        {
            while (track.PlayState != PlayState.Stopped)
            {
                if (track.PlayState == PlayState.Playing)
                {
                    var samples = audioMixer.Read();
                    track.Write(samples, 0, 1600);
                }
            }
        }
    }

    public class AudioMixer : IDisposable
    {
        private byte[] Buffer = new byte[1600];

        public void AddSamples(byte[] samples)
        {
            var mixedArray = new byte[1600];
            for (int i = 0; i < Buffer.Length; i++)
            {
                short mixed = (short)(Buffer[i] + samples[i]);
                if (mixed > 32767) mixed = 32767;
                if (mixed < -32768) mixed = -32768;
                mixedArray[i] = (byte)mixed;
            }
            Buffer = mixedArray;
        }

        public byte[] Read()
        {
            var bytes = Buffer;
            Buffer = new byte[1600];
            return bytes;
        }

        public void Dispose()
        {
            Buffer = null;
        }
    }
}
