using Android.Media;
using NAudio.CoreAudioApi;
using System;
using System.Threading;

namespace NAudio.Wave
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

            //Set the encoding
            if (WaveFormat.Encoding == WaveFormatEncoding.Pcm || WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                encoding = WaveFormat.BitsPerSample switch
                {
                    8 => Encoding.Pcm8bit,
                    16 => Encoding.Pcm16bit,
                    32 => Encoding.PcmFloat,
                    _ => throw new ArgumentException("Input wave provider must be 8-bit, 16-bit or 32bit", nameof(WaveFormat))
                };
            }
            else
            {
                throw new ArgumentException("Input wave provider must be PCM or IEEE Float", nameof(WaveFormat));
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
                if (audioRecord.RecordingState != RecordState.Stopped)
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
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
            {
                bufferSize -= bufferSize % WaveFormat.BlockAlign;
            }

            WaveBuffer waveBuffer = new WaveBuffer(bufferSize);
            captureState = CaptureState.Capturing;

            //Run the record loop
            while (captureState != CaptureState.Stopped)
            {
                if (captureState != CaptureState.Capturing)
                {
                    Thread.Sleep(10);
                    continue;
                }

                if (WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                {
                    var bytesRead = audioRecord.Read(waveBuffer.ByteBuffer, 0, bufferSize);
                    if (bytesRead > 0)
                    {
                        DataAvailable?.Invoke(this, new WaveInEventArgs(waveBuffer.ByteBuffer, bytesRead));
                    }
                }
                else if (WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
                {
                    float[] floatBuffer = new float[bufferSize];
                    var bytesRead = audioRecord.Read(floatBuffer, 0, bufferSize, 1);
                    if (bytesRead > 0)
                    {
                        Buffer.BlockCopy(floatBuffer, 0, waveBuffer.FloatBuffer, 0, bufferSize);
                        DataAvailable?.Invoke(this, new WaveInEventArgs(waveBuffer.ByteBuffer, bufferSize));
                    }
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