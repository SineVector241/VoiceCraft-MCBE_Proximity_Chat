using NAudio.Wave;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceCraft.Core.Audio.Streams
{
    public class VoiceCraftStream : IWaveProvider, IDisposable
    {
        public WaveFormat WaveFormat { get; set; }
        private BufferedWaveProvider DecodedAudio { get; set; }
        private OpusStream OpusStream { get; set; }
        private Task DecodeThread { get; set; }
        private readonly CancellationTokenSource TokenSource;
        private readonly CancellationToken Token;

        public VoiceCraftStream(WaveFormat WaveFormat, OpusStream OpusStream)
        {
            this.WaveFormat = WaveFormat;
            DecodedAudio = new BufferedWaveProvider(WaveFormat) { ReadFully = true, BufferDuration = TimeSpan.FromSeconds(2) };
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
                while (!Token.IsCancellationRequested)
                {
                    var buffer = new byte[WaveFormat.ConvertLatencyToByteSize(40)];
                    var count = OpusStream.Read(buffer, 0, buffer.Length);
                    DecodedAudio.AddSamples(buffer, 0, count);
                }
            }, Token);
        }

        public void Dispose()
        {
            TokenSource.Cancel();
            TokenSource.Dispose();
            DecodeThread.Dispose();
        }
    }
}
