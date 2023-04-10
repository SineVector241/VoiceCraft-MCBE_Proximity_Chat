using Android.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Threading;

namespace VoiceCraft_Mobile.Audio
{
    public class AudioRecorder : IWaveIn
    {
        #region Private Fields
        private AudioRecord audioRecord;
        private SynchronizationContext synchronizationContext;
        private CaptureState captureState;
        private bool disposed;
        #endregion

        #region Public Fields
        public WaveFormat WaveFormat { get; set; }
        public int BufferMilliseconds { get; set; }
        public int NumberOfBuffers { get; set; }
        public AudioSource audioSource { get; set; }
        #endregion

        public event EventHandler<WaveInEventArgs> DataAvailable;
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        #region Constructor
        public AudioRecorder()
        {
            audioRecord = null;
            synchronizationContext = SynchronizationContext.Current;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            NumberOfBuffers = 3;
            captureState = CaptureState.Stopped;
            disposed = false;
            audioSource = AudioSource.Mic;

        }
        #endregion
        #region Private Methods
        private void OpenRecorder()
        {
            //We want to make sure the recorder is definitely closed.
            CloseRecorder();
            Encoding encoding;
            ChannelIn channelMask;

            //Set the encoding. Cannot use IEEFloat otherwise I would have to downgrade float[] to byte[] and I can't be bothered
            if (WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                encoding = WaveFormat.BitsPerSample switch
                {
                    8 => Encoding.Pcm8bit,
                    16 => Encoding.Pcm16bit,
                    _ => throw new ArgumentException("Input wave provider must be 8-bit, 16-bit", nameof(WaveFormat))
                };
            }
            else
            {
                throw new ArgumentException("Input wave provider must be PCM", nameof(WaveFormat));
            }

            //Set the channel type. Only accepts Mono or Stereo
            channelMask = WaveFormat.Channels switch
            {
                1 => ChannelIn.Mono,
                2 => ChannelIn.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(WaveFormat))
            };

            //Determine the buffer size
            int minBufferSize = AudioRecord.GetMinBufferSize(WaveFormat.SampleRate, channelMask, encoding);
            int bufferSize = WaveFormat.ConvertLatencyToByteSize(BufferMilliseconds);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }

            //Create the AudioRecord Object.
            audioRecord = new AudioRecord(audioSource, WaveFormat.SampleRate, channelMask, encoding, bufferSize);
        }

        private void CloseRecorder()
        {
            //Make sure that the recorder was opened
            if (audioRecord != null)
            {
                //Make sure that the recorder is stopped.
                if (audioRecord.RecordingState == RecordState.Stopped)
                    audioRecord.Stop();

                //Release and dispose of everything.
                audioRecord.Release();
                audioRecord.Dispose();
                audioRecord = null;
            }
        }

        private void RecordThread()
        {
            Exception exception = null;
            try
            {
                RecordingLogic();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                captureState = CaptureState.Stopped;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        private void RecordingLogic()
        {
            //Initialize the wave buffer
            int waveBufferSize = (audioRecord.BufferSizeInFrames + NumberOfBuffers - 1) / NumberOfBuffers * WaveFormat.BlockAlign;
            waveBufferSize = (waveBufferSize + 3) & ~3;
            WaveBuffer waveBuffer = new WaveBuffer(waveBufferSize);
            waveBuffer.ByteBufferCount = waveBufferSize;
            captureState = CaptureState.Capturing;

            //Run the record loop
            while (captureState != CaptureState.Stopped)
            {
                if (captureState != CaptureState.Capturing)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var bytesRead = audioRecord.Read(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
                if(bytesRead > 0)
                {
                    if (bytesRead < waveBuffer.ByteBufferCount)
                    {
                        waveBuffer.ByteBufferCount = (bytesRead + 3) & ~3;
                        Array.Clear(waveBuffer.ByteBuffer, bytesRead, waveBuffer.ByteBufferCount - bytesRead);
                    }

                    DataAvailable?.Invoke(this, new WaveInEventArgs(waveBuffer.ByteBuffer, bytesRead));
                }
            }
        }

        private void RaiseRecordingStoppedEvent(Exception e)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                if (synchronizationContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    synchronizationContext.Post(state => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
        #endregion

        #region Public Methods
        public void StartRecording()
        {
            //Check if we haven't disposed.
            ThrowIfDisposed();
            //Check if we are already recording.
            if (captureState == CaptureState.Capturing)
            {
                return;
            }

            //Make sure that we have some format to use.
            if (WaveFormat == null)
            {
                throw new ArgumentNullException(nameof(WaveFormat));
            }

            //Starting capture procedure
            OpenRecorder();
            captureState = CaptureState.Starting;
            audioRecord.StartRecording();
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
        }

        public void StopRecording()
        {
            ThrowIfDisposed();

            //Check if it has already been stopped
            if (captureState != CaptureState.Stopped)
            {
                captureState = CaptureState.Stopped;
                audioRecord.Stop();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            //Clean up any managed and unmanaged resources
            if (!disposed)
            {
                if (disposing)
                {
                    if (captureState != CaptureState.Stopped)
                    {
                        StopRecording();
                    }
                    audioRecord?.Release();
                    audioRecord?.Dispose();
                }
                disposed = true;
            }
        }
    }
}