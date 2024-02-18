using System;
using System.Runtime.InteropServices;

namespace VoiceCraft.Core.Opus
{
    public unsafe class OpusDecoder
    {
        protected IntPtr _ptr;
        protected bool _isDisposed;

        private int SampleBytes;
        private int FrameMilliseconds;
        private int SamplingRate;
        private int Channels;

        private int FrameSamplesPerChannel;
        private int FrameSamples;
        private int FrameBytes;

        [DllImport("opus", EntryPoint = "opus_decoder_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateDecoder(int Fs, int channels, out OpusError error);
        [DllImport("opus", EntryPoint = "opus_decoder_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern void DestroyDecoder(IntPtr decoder);
        [DllImport("opus", EntryPoint = "opus_decode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Decode(IntPtr st, byte* data, int len, byte* pcm, int max_frame_size, int decode_fec);
        [DllImport("opus", EntryPoint = "opus_decoder_ctl", CallingConvention = CallingConvention.Cdecl)]
        private static extern int DecoderCtl(IntPtr st, OpusCtl request, int value);

        public OpusDecoder(int SamplingRate, int Channels, int FrameMilliseconds)
        {
            this.SamplingRate = SamplingRate;
            this.Channels = Channels;
            this.FrameMilliseconds = FrameMilliseconds;

            SampleBytes = sizeof(short) * Channels;
            FrameSamplesPerChannel = SamplingRate / 1000 * FrameMilliseconds;
            FrameSamples = FrameSamplesPerChannel * Channels;
            FrameBytes = FrameSamplesPerChannel * SampleBytes;

            _ptr = CreateDecoder(SamplingRate, Channels, out var error);
            CheckError(error);
        }

        public unsafe int DecodeFrame(byte[] input, int inputOffset, int inputCount, byte[] output, int outputOffset, bool decodeFEC)
        {
            int result = 0;
            fixed (byte* inPtr = input)
            fixed (byte* outPtr = output)
                result = Decode(_ptr, inPtr + inputOffset, inputCount, outPtr + outputOffset, FrameSamplesPerChannel, decodeFEC ? 1 : 0);
            CheckError(result);
            return result * SampleBytes;
        }

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (_ptr != IntPtr.Zero)
                    DestroyDecoder(_ptr);

                if (!_isDisposed)
                    _isDisposed = true;
            }
        }

        ~OpusDecoder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static void CheckError(int result)
        {
            if (result < 0)
                throw new Exception($"Opus Error: {(OpusError)result}");
        }
        protected static void CheckError(OpusError error)
        {
            if ((int)error < 0)
                throw new Exception($"Opus Error: {error}");
        }
    }
}