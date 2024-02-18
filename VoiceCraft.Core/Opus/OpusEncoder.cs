using System;
using System.Runtime.InteropServices;

namespace VoiceCraft.Core.Opus
{
    public unsafe class OpusEncoder
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

        [DllImport("opus", EntryPoint = "opus_encoder_create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CreateEncoder(int Fs, int channels, int application, out OpusError error);
        [DllImport("opus", EntryPoint = "opus_encoder_destroy", CallingConvention = CallingConvention.Cdecl)]
        private static extern void DestroyEncoder(IntPtr encoder);
        [DllImport("opus", EntryPoint = "opus_encode", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Encode(IntPtr st, byte* pcm, int frame_size, byte* data, int max_data_bytes);
        [DllImport("opus", EntryPoint = "opus_encoder_ctl", CallingConvention = CallingConvention.Cdecl)]
        private static extern OpusError EncoderCtl(IntPtr st, OpusCtl request, int value);

        public AudioApplication Application { get; }
        public int BitRate { get; }

        public OpusEncoder(int bitrate, AudioApplication application, int packetLoss, int SamplingRate, int Channels, int FrameMilliseconds)
        {
            if (bitrate < 1)
                throw new ArgumentOutOfRangeException(nameof(bitrate));

            this.SamplingRate = SamplingRate;
            this.Channels = Channels;
            this.FrameMilliseconds = FrameMilliseconds;

            SampleBytes = sizeof(short) * Channels;
            FrameSamplesPerChannel = SamplingRate / 1000 * FrameMilliseconds;
            FrameSamples = FrameSamplesPerChannel * Channels;
            FrameBytes = FrameSamplesPerChannel * SampleBytes;

            Application = application;
            BitRate = bitrate;

            OpusApplication opusApplication;
            OpusSignal opusSignal;
            switch (application)
            {
                case AudioApplication.Mixed:
                    opusApplication = OpusApplication.MusicOrMixed;
                    opusSignal = OpusSignal.Auto;
                    break;
                case AudioApplication.Music:
                    opusApplication = OpusApplication.MusicOrMixed;
                    opusSignal = OpusSignal.Music;
                    break;
                case AudioApplication.Voice:
                    opusApplication = OpusApplication.Voice;
                    opusSignal = OpusSignal.Voice;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(application));
            }

            _ptr = CreateEncoder(SamplingRate, Channels, (int)opusApplication, out var error);
            CheckError(error);
            CheckError(EncoderCtl(_ptr, OpusCtl.SetSignal, (int)opusSignal));
            CheckError(EncoderCtl(_ptr, OpusCtl.SetPacketLossPercent, packetLoss)); //%
            CheckError(EncoderCtl(_ptr, OpusCtl.SetBitrate, bitrate));
        }

        public unsafe int EncodeFrame(byte[] input, int inputOffset, byte[] output, int outputOffset)
        {
            int result = 0;
            fixed (byte* inPtr = input)
            fixed (byte* outPtr = output)
                result = Encode(_ptr, inPtr + inputOffset, FrameSamplesPerChannel, outPtr + outputOffset, output.Length - outputOffset);
            CheckError(result);
            return result;
        }

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (_ptr != IntPtr.Zero)
                    DestroyEncoder(_ptr);

                if (!_isDisposed)
                    _isDisposed = true;
            }
        }

        ~OpusEncoder()
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