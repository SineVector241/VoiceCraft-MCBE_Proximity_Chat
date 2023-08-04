using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VoiceCraft.Mobile.Network
{
    public class VoiceCraftParticipant
    {
        private readonly int BufferSize;

        public string Name { get; }
        public uint PacketCount { get; private set; }
        public BufferedWaveProvider AudioBuffer;
        public Wave16ToFloatProvider FloatProvider;
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

            AudioBuffer = new BufferedWaveProvider(WaveFormat) { DiscardOnBufferOverflow = true };
            FloatProvider = new Wave16ToFloatProvider(AudioBuffer);
            AudioProvider = new MonoToStereoSampleProvider(FloatProvider.ToSampleProvider());
            OpusDecoder = new OpusDecoder(WaveFormat.SampleRate, WaveFormat.Channels);
        }

        public void AddAudioSamples(byte[] Audio, uint PacketCount)
        {
            byte[] audioFrame = new byte[BufferSize];

            bool packetsLost = PacketCount - this.PacketCount != 1;
            short[] decoded = new short[BufferSize / 2];
            try
            {
                //Decode or Enable FEC if packets are lost.
                if (packetsLost)
                {
                    //Decode packet with FEC ON
                    OpusDecoder.Decode(Audio, 0, Audio.Length, decoded, 0, decoded.Length, true);
                    audioFrame = ShortsToBytes(decoded, 0, decoded.Length);
                    AudioBuffer.AddSamples(audioFrame, 0, audioFrame.Length);

                    //Decode packet with FEC OFF
                    OpusDecoder.Decode(Audio, 0, Audio.Length, decoded, 0, decoded.Length, false);
                }
                else
                {
                    OpusDecoder.Decode(Audio, 0, Audio.Length, decoded, 0, decoded.Length);
                }
                audioFrame = ShortsToBytes(decoded, 0, decoded.Length);
            }
            //Declare as lost/corrupted frame. We'll just drop the packet and do nothing by returning.
            catch {
                return;
            }

            AudioBuffer.AddSamples(audioFrame, 0, audioFrame.Length);
            this.PacketCount = PacketCount;
        }

        public void SetVolume(float Volume)
        {
            FloatProvider.Volume = Volume;
        }

        //Private Methods
        private static byte[] ShortsToBytes(short[] input, int offset, int length)
        {
            byte[] processedValues = new byte[length * 2];
            for (int c = 0; c < length; c++)
            {
                processedValues[c * 2] = (byte)(input[c + offset] & 0xFF);
                processedValues[c * 2 + 1] = (byte)((input[c + offset] >> 8) & 0xFF);
            }

            return processedValues;
        }
    }
}