using Concentus.Structs;
using NAudio.Wave;
using System;

namespace VoiceCraft.Core.Audio.Streams
{
    public class OpusStream : IWaveProvider
    {
        public WaveFormat WaveFormat { get; set; }
        private readonly OpusDecoder Decoder;
        private readonly short[] DecodeBuffer;
        private bool NextMissed;

        public OpusStream(WaveFormat WaveFormat)
        {
            this.WaveFormat = WaveFormat;
            DecodeBuffer = new short[WaveFormat.ConvertLatencyToByteSize(40) / 2];
            Decoder = new OpusDecoder(WaveFormat.SampleRate, 1);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            count = !NextMissed || count > 0
                ? Decoder.Decode(buffer, offset, count, DecodeBuffer, 0, DecodeBuffer.Length, false)
                : Decoder.Decode(null, 0, 0, DecodeBuffer, 0, DecodeBuffer.Length, false);

            //Convert and put into the buffer.
            Buffer.BlockCopy(ShortsToBytes(DecodeBuffer, 0, count), 0, buffer, 0, count);
            return count;
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
