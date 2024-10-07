﻿using NAudio.Wave;
using System;
using VoiceCraft.Client.PDK.Audio;

namespace VoiceCraft.Client.Windows.Audio
{
    public class AudioPlayer : IAudioPlayer
    {
        private readonly WaveOutEvent _nativePlayer = new WaveOutEvent();

        public PlaybackState PlaybackState => _nativePlayer.PlaybackState;
        public WaveFormat OutputWaveFormat => _nativePlayer.OutputWaveFormat;
        public float Volume { get => _nativePlayer.Volume; set => _nativePlayer.Volume = value; }
        public int DesiredLatency { get => _nativePlayer.DesiredLatency; set => _nativePlayer.DesiredLatency = value; }

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public AudioPlayer()
        {
            _nativePlayer.PlaybackStopped += InvokePlaybackStopped;
        }

        public void SetDevice(string device)
        {
            for (int n = 0; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                if (caps.ProductName == device)
                {
                    _nativePlayer.DeviceNumber = n;
                    return;
                }
            }

            _nativePlayer.DeviceNumber = -1;
        }

        public void Init(IWaveProvider waveProvider)
        {
            _nativePlayer.Init(waveProvider);
        }

        public void Play()
        {
            _nativePlayer.Play();
        }

        public void Pause()
        {
            _nativePlayer.Pause();
        }

        public void Stop()
        {
            _nativePlayer.Stop();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void InvokePlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(sender, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nativePlayer.Dispose();
            }
        }
    }
}