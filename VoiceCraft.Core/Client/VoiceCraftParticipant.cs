using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using VoiceCraft.Core.Audio;

namespace VoiceCraft.Core.Client
{
    public class VoiceCraftParticipant
    {
        private readonly int BufferSize;
        public DateTime LastSpoke { get; private set; }

        private float volume = 1.0f;
        private float proximityVolume = 0.0f;

        public bool Muted = false;
        public bool Deafened = false;

        public string Name { get; }
        public float Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                UpdateVolume();
            }
        }
        public float ProximityVolume
        {
            get { return proximityVolume; }
            set
            {
                proximityVolume = value;
                UpdateVolume();
            }
        }
        public uint PacketCount { get; private set; }
        public BufferedWaveProvider AudioBuffer;
        public Wave16ToFloatProvider FloatProvider;
        public EchoSampleProvider EchoProvider;
        public MonoToStereoSampleProvider AudioProvider { get; }
        public OpusDecoder OpusDecoder { get; }

        public VoiceCraftParticipant(string Name, WaveFormat WaveFormat, int RecordLengthMS)
        {
            this.Name = Name;

            BufferSize = RecordLengthMS * WaveFormat.AverageBytesPerSecond / 1000;
            if (BufferSize % WaveFormat.BlockAlign != 0)
            {
                BufferSize -= BufferSize % WaveFormat.BlockAlign;
            }

            //Setup and wire everything up.
            AudioBuffer = new BufferedWaveProvider(WaveFormat) { DiscardOnBufferOverflow = true };
            FloatProvider = new Wave16ToFloatProvider(AudioBuffer);
            EchoProvider = new EchoSampleProvider(FloatProvider.ToSampleProvider());
            AudioProvider = new MonoToStereoSampleProvider(EchoProvider);
            OpusDecoder = new OpusDecoder(WaveFormat.SampleRate, WaveFormat.Channels);
        }

        public void AddAudioSamples(byte[] Audio, uint PacketCount)
        {
            uint packetsLost = PacketCount - (this.PacketCount + 1);
            short[] decoded = new short[BufferSize / 2];
            try
            {
                byte[] audioFrame = new byte[BufferSize];

                if (packetsLost == 0)
                {
                    OpusDecoder.Decode(Audio, 0, Audio.Length, decoded, 0, decoded.Length);
                }
                else if (packetsLost < 0) //Packet lost.
                {
                    //Decode packet with FEC ON
                    OpusDecoder.Decode(Audio, 0, Audio.Length, decoded, 0, decoded.Length, true);
                    audioFrame = ShortsToBytes(decoded, 0, decoded.Length);
                    AudioBuffer.AddSamples(audioFrame, 0, audioFrame.Length);

                    //Decode packet with FEC OFF
                    OpusDecoder.Decode(Audio, 0, Audio.Length, decoded, 0, decoded.Length, false);
                }

                audioFrame = ShortsToBytes(decoded, 0, decoded.Length);
                AudioBuffer.AddSamples(audioFrame, 0, audioFrame.Length);
                this.PacketCount = PacketCount;
                LastSpoke = DateTime.UtcNow;
            }
            //Declare as lost/corrupted frame. We'll just drop the packet and do nothing by returning.
            catch
            {
                return;
            }
        }

        //Private Methods
        private void UpdateVolume()
        {
            FloatProvider.Volume = proximityVolume * volume;
        }

        private static byte[] ShortsToBytes(short[] input, int offset, int length)
        {
            byte[] processedValues = new byte[length * 2];
            for (int c = 0; c < length; c++)
            {
                processedValues[c * 2] = (byte)(input[c + offset] & 0xFF);
                processedValues[c * 2 + 1] = (byte)(input[c + offset] >> 8 & 0xFF);
            }

            return processedValues;
        }
    }
}