using NAudio.Wave;
using System;
using System.IO;
using System.Threading;
using VoiceCraftProximityChat.Core.Network;

namespace VoiceCraftProximityChat
{
    public static class Audio
    {
        public static WaveIn waveIn = new WaveIn();
        private static BufferedWaveProvider player = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));

        public static void AddSamples(byte[] bytes, float volume)
        {
            for(int i = 0; i < 20; i++)
            {
                Buffer.SetByte(bytes, i, 0);
            }
            var ms = new MemoryStream(bytes);
            var rs = new RawSourceWaveStream(ms, WaveFormat.CreateIeeeFloatWaveFormat(16000, 1));
            var wo = new WaveOutEvent();
            wo.Volume = volume;
            wo.Init(rs);
            wo.Play();
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(50);
            }
            wo.Dispose();
        }

        private static void OnDataAvailable(object? sender, WaveInEventArgs args)
        {
            float max = 0;
            for (int index = 0; index < args.BytesRecorded; index += 2)
            {
                short sample = (short)((args.Buffer[index + 1] << 8) |
                                        args.Buffer[index + 0]);
                var sample32 = sample / 32768f;
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }
            Network.client.Send(new Packet() { VCAudioBuffer = args.Buffer, VCPacketDataIdentifier = PacketIdentifier.AudioStream, VCSessionKey = Network.KEY }.GetPacketDataStream());
        }

        public static void Init()
        {
            waveIn.BufferMilliseconds = 50;
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(16000, 1);
            waveIn.DataAvailable += OnDataAvailable;
            waveIn.StartRecording();
        }
    }
}
