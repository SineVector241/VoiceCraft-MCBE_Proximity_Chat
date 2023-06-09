using Concentus.Structs;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using VoiceCraft.Mobile.Network.Codecs;

namespace VoiceCraft.Mobile.Network
{
    public class VoiceCraftParticipant
    {
        private int BufferSize;

        public string Name { get; }
        public uint PacketCount { get; private set; }
        public AudioCodecs Codec { get; }
        public BufferedWaveProvider AudioBuffer;
        public Wave16ToFloatProvider FloatProvider;
        public MonoToStereoSampleProvider AudioProvider { get; }

#nullable enable
        public OpusDecoder? OpusDecoder { get; }
        public G722ChatCodec? G722Decoder { get; }

        public VoiceCraftParticipant(string Name, WaveFormat WaveFormat, int RecordLengthMS, AudioCodecs Codec)
        {
            this.Name = Name;
            this.Codec = Codec;

            BufferSize = RecordLengthMS * WaveFormat.AverageBytesPerSecond / 1000;
            if (BufferSize % WaveFormat.BlockAlign != 0)
            {
                BufferSize -= BufferSize % WaveFormat.BlockAlign;
            }

            AudioBuffer = new BufferedWaveProvider(WaveFormat) { DiscardOnBufferOverflow = true, BufferDuration = TimeSpan.FromSeconds(5) };
            FloatProvider = new Wave16ToFloatProvider(AudioBuffer);
            AudioProvider = new MonoToStereoSampleProvider(FloatProvider.ToSampleProvider());

            switch(Codec)
            {
                case AudioCodecs.Opus:
                    if (RecordLengthMS != 5 && RecordLengthMS != 10 && RecordLengthMS != 20 && RecordLengthMS != 40 && RecordLengthMS != 60)
                        throw new ArgumentException("Opus can only handle frame sizes of 5ms, 10ms, 20ms, 40ms or 60ms.");

                    OpusDecoder = new OpusDecoder(WaveFormat.SampleRate, WaveFormat.Channels);
                    break;
                case AudioCodecs.G722:
                    if (WaveFormat.SampleRate != 16000) throw new Exception("G722ChatCodec only accepts 16khz audio.");
                    if (WaveFormat.Channels != 1) throw new Exception("G722ChatCodec only accepts mono audio.");
                    G722Decoder = new G722ChatCodec();
                    break;
            }
        }

        public void AddAudioSamples(byte[] Audio, uint PacketCount)
        {
            byte[] audioFrame = new byte[BufferSize];
            switch(Codec)
            {
                case AudioCodecs.Opus:
                    if (OpusDecoder == null)
                        return;

                    bool packetsLost = PacketCount - this.PacketCount != 1;
                    short[] decoded = new short[BufferSize / 2];
                    try
                    {
                        //Decode or Enable PLC if packets are lost.
                        OpusDecoder.Decode(packetsLost ? null : Audio, 0, packetsLost ? 0 : Audio.Length, decoded, 0, decoded.Length);
                        audioFrame = ShortsToBytes(decoded, 0, decoded.Length);
                    }
                    //Declare as lost/corrupted frame.
                    catch 
                    {
                        //Enable PLC
                        OpusDecoder.Decode(null, 0, 0, decoded, 0, decoded.Length);
                    }
                    break;
                case AudioCodecs.G722:
                    if (G722Decoder == null)
                        return;

                    audioFrame = G722Decoder.Decode(Audio, 0, Audio.Length);
                    break;
            }

            AudioBuffer.AddSamples(audioFrame, 0, audioFrame.Length);
            this.PacketCount = PacketCount;
        }

        public void SetVolume(int Volume)
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