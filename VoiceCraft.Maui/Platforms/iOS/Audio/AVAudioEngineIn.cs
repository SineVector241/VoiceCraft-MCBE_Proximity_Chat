using AVFoundation;
using Foundation;
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace NAudio.Wave
{
    public class AVAudioEngineIn : IWaveIn
    {
        #region Private Fields
        private AVAudioEngine audioEngine;
        private AVAudioInputNode inputNode;
        private AVAudioFormat recordingFormat;
        private bool disposed = false;
        private SynchronizationContext synchronizationContext;
        #endregion

        #region Public Fields
        public WaveFormat WaveFormat { get; set; } 
        public int BufferMilliseconds { get; set; }
        #endregion

        #region Constructor
        public AVAudioEngineIn()
        {
            synchronizationContext = SynchronizationContext.Current;
            audioEngine = new AVAudioEngine();
            inputNode = audioEngine.InputNode;
            recordingFormat = inputNode.GetBusOutputFormat(0);
            WaveFormat = new WaveFormat(8000, 16, 1); 
            BufferMilliseconds = 100;
        }
        #endregion

        #region Events
        public event EventHandler<WaveInEventArgs> DataAvailable;
        public event EventHandler<StoppedEventArgs> RecordingStopped;
        #endregion

        #region Private Methods
        private void OpenRecorder()
        {
            // Ensure the engine is stopped before starting a new session
            if (audioEngine.Running)
            {
                audioEngine.Stop();
                inputNode.RemoveTapOnBus(0);
            }

            inputNode.InstallTapOnBus(0, 4096, recordingFormat, (buffer, when) =>
            {
                //todo: not sure if anything is required here
                // Conversion from AVAudioPCMBuffer to byte[] and invoking DataAvailable
                // This is a placeholder. Actual conversion depends on the WaveFormat
                var frameLength = (int)buffer.FrameLength;
                var data = new byte[frameLength];
                // Assuming 16-bit PCM for simplicity
                Marshal.Copy(buffer.FloatChannelData, data, 0, frameLength);
                OnDataAvailable(new WaveInEventArgs(data, frameLength));
            });

            audioEngine.Prepare();
        }

        private void CloseRecorder()
        {
            if (audioEngine.Running)
            {
                audioEngine.Stop();
                inputNode.RemoveTapOnBus(0);
            }
        }

        private void OnDataAvailable(WaveInEventArgs e) => synchronizationContext.Post(_ => DataAvailable?.Invoke(this, e), null);

        private void OnRecordingStopped(StoppedEventArgs e) => synchronizationContext.Post(_ => RecordingStopped?.Invoke(this, e), null);
        #endregion

        #region Public Methods
        public void StartRecording()
        {
            OpenRecorder();
            NSError error;
            audioEngine.StartAndReturnError(out error);

            if (error != null) { 
                var exception = new Exception(error.LocalizedDescription);
                OnRecordingStopped(new StoppedEventArgs(exception));
            }
        }

        public void StopRecording()
        {
            CloseRecorder();
            OnRecordingStopped(new StoppedEventArgs(null));
        }

        public void Dispose()
        {
            if (!disposed)
            {
                CloseRecorder();
                disposed = true;
            }
        }
        #endregion
    }
}
