using Android.Media;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents an Android wave player implemented using <see cref="AudioTrack"/>.
    /// </summary>
    public class AudioTrackOut : IWavePlayer
    {
        #region Fields

        IWaveProvider m_WaveProvider;
        AudioTrack m_AudioTrack;
        Thread m_PlaybackThread;
        float m_Volume;
        bool m_IsDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current playback state.
        /// </summary>
        public PlaybackState PlaybackState { get; private set; }

        /// <summary>
        /// Gets or sets the volume in % (0.0 to 1.0).
        /// </summary>
        public float Volume
        {
            get => m_Volume;
            set
            {
                m_Volume = (value < 0.0f) ? 0.0f : (value > 1.0f) ? 1.0f : value;
                m_AudioTrack?.SetVolume(m_Volume);
            }
        }

        /// <summary>
        /// Gets or sets the desired latency in milliseconds.
        /// </summary>
        public int DesiredLatency { get; set; }

        /// <summary>
        /// Gets or sets the number of buffers to use.
        /// </summary>
        public int NumberOfBuffers { get; set; }

        /// <summary>
        /// Gets or sets the usage.
        /// </summary>
        public AudioUsageKind Usage { get; set; }

        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        public AudioContentType ContentType { get; set; }

        /// <summary>
        /// Gets or sets the performance mode.
        /// </summary>
        public AudioTrackPerformanceMode PerformanceMode { get; set; }

        public WaveFormat OutputWaveFormat { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the player has stopped.
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioTrackOut"/> class.
        /// </summary>
        public AudioTrackOut()
        {
            //Initialize the fields and properties
            m_WaveProvider = null;
            m_AudioTrack = null;
            m_PlaybackThread = null;
            m_Volume = 1.0f;
            m_IsDisposed = false;
            PlaybackState = PlaybackState.Stopped;
            DesiredLatency = 300;
            NumberOfBuffers = 2;
            Usage = AudioUsageKind.Media;
            ContentType = AudioContentType.Music;
            PerformanceMode = AudioTrackPerformanceMode.None;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the current instance of the <see cref="AudioTrackOut"/> class.
        /// </summary>
        ~AudioTrackOut()
        {
            //Dispose of this object
            Dispose(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the player with the specified wave provider.
        /// </summary>
        /// <param name="waveProvider">The wave provider to be played.</param>
        public void Init(IWaveProvider waveProvider)
        {
            //Make sure we haven't been disposed
            ThrowIfDisposed();

            //Check the player state
            if (m_WaveProvider != null)
            {
                throw new InvalidOperationException("This wave player instance has already been initialized");
            }

            //Initialize the wave provider
            Encoding encoding;
            if (waveProvider == null)
            {
                throw new ArgumentNullException(nameof(waveProvider));
            }
            else if (waveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm || waveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                encoding = waveProvider.WaveFormat.BitsPerSample switch
                {
                    8 => Encoding.Pcm8bit,
                    16 => Encoding.Pcm16bit,
                    32 => Encoding.PcmFloat,
                    _ => throw new ArgumentException("Input wave provider must be 8-bit, 16-bit, or 32-bit", nameof(waveProvider))
                };
            }
            else
            {
                throw new ArgumentException("Input wave provider must be PCM or IEEE float", nameof(waveProvider));
            }
            m_WaveProvider = waveProvider;

            //Determine the channel mask
            ChannelOut channelMask = m_WaveProvider.WaveFormat.Channels switch
            {
                1 => ChannelOut.Mono,
                2 => ChannelOut.Stereo,
                _ => throw new ArgumentException("Input wave provider must be mono or stereo", nameof(waveProvider))
            };

            //Determine the buffer size
            int minBufferSize = AudioTrack.GetMinBufferSize(m_WaveProvider.WaveFormat.SampleRate, channelMask, encoding);
            int bufferSize = m_WaveProvider.WaveFormat.ConvertLatencyToByteSize(DesiredLatency);
            if (bufferSize < minBufferSize)
            {
                bufferSize = minBufferSize;
            }

            //Initialize the audio track
            m_AudioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(Usage)
                    .SetContentType(ContentType)
                    .Build())
                .SetAudioFormat(new AudioFormat.Builder()
                    .SetEncoding(encoding)
                    .SetSampleRate(m_WaveProvider.WaveFormat.SampleRate)
                    .SetChannelMask(channelMask)
                    .Build())
                .SetBufferSizeInBytes(bufferSize)
                .SetTransferMode(AudioTrackMode.Stream)
                .SetPerformanceMode(PerformanceMode)
                .Build();
            m_AudioTrack.SetVolume(Volume);
        }

        /// <summary>
        /// Starts the player.
        /// </summary>
        public void Play()
        {
            //Make sure we haven't been disposed
            ThrowIfDisposed();

            //Check the player state
            ThrowIfNotInitialized();
            if (PlaybackState == PlaybackState.Playing)
            {
                return;
            }

            //Start the wave player
            PlaybackState = PlaybackState.Playing;
            m_AudioTrack.Play();
            if (m_PlaybackThread == null || !m_PlaybackThread.IsAlive)
            {
                m_PlaybackThread = new Thread(PlaybackThread)
                {
                    Priority = ThreadPriority.Highest
                };
                m_PlaybackThread.Start();
            }
        }

        /// <summary>
        /// Pauses the player.
        /// </summary>
        public void Pause()
        {
            //Make sure we haven't been disposed
            ThrowIfDisposed();

            //Check the player state
            ThrowIfNotInitialized();
            if (PlaybackState == PlaybackState.Stopped || PlaybackState == PlaybackState.Paused)
            {
                return;
            }

            //Pause the wave player
            PlaybackState = PlaybackState.Paused;
            m_AudioTrack.Pause();
        }

        /// <summary>
        /// Stops the player.
        /// </summary>
        public void Stop()
        {
            //Make sure we haven't been disposed
            ThrowIfDisposed();

            //Check the player state
            ThrowIfNotInitialized();
            if (PlaybackState == PlaybackState.Stopped)
            {
                return;
            }

            //Stop the wave player
            PlaybackState = PlaybackState.Stopped;
            m_AudioTrack.Stop();
            m_PlaybackThread.Join();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="AudioTrackOut"/> class.
        /// </summary>
        public void Dispose()
        {
            //Dispose of this object
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Raises the <see cref="PlaybackStopped"/> event with the provided arguments.
        /// </summary>
        /// <param name="exception">An optional exception that occured.</param>
        protected virtual void OnPlaybackStopped(Exception exception = null)
        {
            //Raise the playback stopped event
            PlaybackStopped?.Invoke(this, new StoppedEventArgs(exception));
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="AudioTrackOut"/>, and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            //Clean up any managed and unmanaged resources
            if (!m_IsDisposed)
            {
                if (disposing)
                {
                    if (PlaybackState != PlaybackState.Stopped)
                    {
                        Stop();
                    }
                    m_AudioTrack?.Release();
                    m_AudioTrack?.Dispose();
                }
                m_IsDisposed = true;
            }
        }

        #endregion

        #region Private Methods

        private void PlaybackThread()
        {
            //Run the playback logic
            Exception exception = null;
            try
            {
                PlaybackLogic();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                PlaybackState = PlaybackState.Stopped;
                OnPlaybackStopped(exception);
            }
        }

        private void PlaybackLogic()
        {
            //Initialize the wave buffer
            int waveBufferSize = (m_AudioTrack.BufferSizeInFrames + NumberOfBuffers - 1) / NumberOfBuffers * m_WaveProvider.WaveFormat.BlockAlign;
            waveBufferSize = (waveBufferSize + 3) & ~3;
            WaveBuffer waveBuffer = new(waveBufferSize)
            {
                ByteBufferCount = waveBufferSize
            };

            //Run the playback loop
            while (PlaybackState != PlaybackState.Stopped)
            {
                //Check the playback state
                if (PlaybackState != PlaybackState.Playing)
                {
                    Thread.Sleep(10);
                    continue;
                }

                //Fill the wave buffer with new samples
                int bytesRead = m_WaveProvider.Read(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
                if (bytesRead > 0)
                {
                    //Clear the unused space in the wave buffer if necessary
                    if (bytesRead < waveBuffer.ByteBufferCount)
                    {
                        waveBuffer.ByteBufferCount = (bytesRead + 3) & ~3;
                        Array.Clear(waveBuffer.ByteBuffer, bytesRead, waveBuffer.ByteBufferCount - bytesRead);
                    }

                    //Write the wave buffer to the audio track
                    WriteBuffer(waveBuffer);
                }
                else
                {
                    //Stop the audio track
                    m_AudioTrack.Stop();
                    break;
                }
            }

            //Flush the audio track
            m_AudioTrack.Flush();
        }

        private void WriteBuffer(IWaveBuffer waveBuffer)
        {
            //Write the specified wave buffer to the audio track
            if (m_WaveProvider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                m_AudioTrack.Write(waveBuffer.ByteBuffer, 0, waveBuffer.ByteBufferCount);
            }
            else if (m_WaveProvider.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                //AudioTrack.Write doesn't appreciate WaveBuffer.FloatBuffer
                float[] floatBuffer = new float[waveBuffer.FloatBufferCount];
                for (int i = 0; i < waveBuffer.FloatBufferCount; i++)
                {
                    floatBuffer[i] = waveBuffer.FloatBuffer[i];
                }
                m_AudioTrack.Write(floatBuffer, 0, floatBuffer.Length, WriteMode.Blocking);
            }
        }

        private void ThrowIfNotInitialized()
        {
            //Throw an exception if this object has not been initialized
            if (m_WaveProvider == null)
            {
                throw new InvalidOperationException("This wave player instance has not been initialized");
            }
        }

        private void ThrowIfDisposed()
        {
            //Throw an exception if this object has been disposed
            if (!m_IsDisposed)
            {
                return;
            }
            throw new ObjectDisposedException(GetType().FullName);
        }

        #endregion
    }
}