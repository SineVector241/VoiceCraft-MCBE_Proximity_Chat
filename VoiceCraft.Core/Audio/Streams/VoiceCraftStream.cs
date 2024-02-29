using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceCraft.Core.Audio.Streams
{
    public class VoiceCraftStream : IWaveProvider, IDisposable
    {
        public WaveFormat WaveFormat { get; set; }
        private PreloadedBufferedWaveProvider DecodedAudio { get; set; }
        private VoiceCraftJitterBuffer JitterBuffer { get; set; }
        private Task DecodeThread { get; set; }
        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        public VoiceCraftStream(WaveFormat WaveFormat, VoiceCraftJitterBuffer JitterBuffer)
        {
            this.WaveFormat = WaveFormat;
            DecodedAudio = new PreloadedBufferedWaveProvider(WaveFormat) { ReadFully = true, BufferDuration = TimeSpan.FromSeconds(2), DiscardOnBufferOverflow = true, PreloadedBytesTarget = WaveFormat.ConvertLatencyToByteSize(60) };
            this.JitterBuffer = JitterBuffer;
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;

            DecodeThread = Run();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return DecodedAudio.Read(buffer, offset, count);
        }

        private Task Run()
        {
            return Task.Run(async() =>
            {
                long startTick = Environment.TickCount;
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        long tick = Environment.TickCount;
                        long dist = startTick - tick;
                        if (dist > 0)
                        {
                            await Task.Delay((int)dist).ConfigureAwait(false);
                            continue;
                        }
                        startTick += JitterBuffer.FrameSizeMS;

                        var buffer = new byte[WaveFormat.ConvertLatencyToByteSize(JitterBuffer.FrameSizeMS)];
                        var count = JitterBuffer.Get(buffer);
                        if (count > 0)
                        {
                            DecodedAudio.AddSamples(buffer, 0, count);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }, Token);
        }

        public void Dispose()
        {
            TokenSource.Cancel();
            DecodeThread.Wait();
            TokenSource.Dispose();
            DecodeThread.Dispose();
        }
    }
}
