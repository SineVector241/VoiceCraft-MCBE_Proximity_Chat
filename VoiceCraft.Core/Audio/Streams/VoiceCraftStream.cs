using NAudio.Wave;
using System;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Client;

namespace VoiceCraft.Core.Audio.Streams
{
    public class VoiceCraftStream : IWaveProvider, IDisposable
    {
        public WaveFormat WaveFormat { get; set; }
        private BufferedWaveProvider DecodedAudio { get; set; }
        private OpusStream OpusStream { get; set; }
        private Task DecodeThread { get; set; }
        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        public VoiceCraftStream(WaveFormat WaveFormat, OpusStream OpusStream)
        {
            this.WaveFormat = WaveFormat;
            DecodedAudio = new BufferedWaveProvider(WaveFormat) { ReadFully = true, BufferDuration = TimeSpan.FromSeconds(2), DiscardOnBufferOverflow = true };
            this.OpusStream = OpusStream;
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
            return Task.Run(() => {
                var nextTick = Environment.TickCount;
                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        var buffer = new byte[WaveFormat.ConvertLatencyToByteSize(VoiceCraftClient.FrameMilliseconds)];
                        var count = OpusStream.Read(buffer, 0, buffer.Length);
                        if (count > 0)
                        {
                            DecodedAudio.AddSamples(buffer, 0, count);
                        }
                    }
                    catch (Exception ex) { }
                }
            }, Token);
        }

        public void Dispose()
        {
            TokenSource.Cancel();
            DecodeThread.Wait();
            TokenSource.Dispose();
            DecodeThread.Dispose();
            OpusStream.Dispose();
        }
    }
}
