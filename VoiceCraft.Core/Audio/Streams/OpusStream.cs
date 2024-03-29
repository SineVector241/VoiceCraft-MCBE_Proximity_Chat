﻿using NAudio.Wave;
using System;
using VoiceCraft.Core.Client;
using VoiceCraft.Core.Opus;

namespace VoiceCraft.Core.Audio.Streams
{
    public class OpusStream : IWaveProvider, IDisposable
    {
        public WaveFormat WaveFormat { get; set; }
        private readonly JitterBuffer JitterBuffer;
        private readonly OpusDecoder Decoder;
        private byte[] DecodeBuffer;
        private JitterBufferPacket outPacket;

        public OpusStream(WaveFormat WaveFormat, JitterBuffer JitterBuffer)
        {
            this.WaveFormat = WaveFormat;
            this.JitterBuffer = JitterBuffer;
            DecodeBuffer = new byte[WaveFormat.ConvertLatencyToByteSize(VoiceCraftClient.FrameMilliseconds)];
            Decoder = new OpusDecoder(WaveFormat.SampleRate, 1, VoiceCraftClient.FrameMilliseconds);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (outPacket.Data == null)
            {
                outPacket.Data = new byte[buffer.Length];
            }
            else
                Array.Clear(outPacket.Data, 0, outPacket.Data.Length);

            var shortsRead = 0;
            var status = JitterBuffer.Get(ref outPacket);
            if (status == 0)
            {
                shortsRead = Decoder.DecodeFrame(outPacket.Data, 0, outPacket.Length, DecodeBuffer, 0, false);
            }
            else if(status == -1)
            {
                return 0;
            }
            else
            {
                // no packet found
                shortsRead = Decoder.DecodeFrame(null, 0, 0, DecodeBuffer, 0, false);
            }

            //Convert and put into the buffer.
            Buffer.BlockCopy(DecodeBuffer, 0, buffer, 0, DecodeBuffer.Length);
            return DecodeBuffer.Length;
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

        public void Dispose()
        {
            Decoder.Dispose();
        }
    }
}
