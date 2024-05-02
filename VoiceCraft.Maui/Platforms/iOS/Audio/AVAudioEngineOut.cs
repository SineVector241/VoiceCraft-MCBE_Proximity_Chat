using AVFoundation;
using Foundation;

namespace NAudio.Wave
{
    /// <summary>
    /// Represents an iOS wave player implemented using <see cref="AVAudioEngine"/>.
    /// </summary>
    public class AVAudioEngineOut : IWavePlayer
    {
        #region Fields

        IWaveProvider m_WaveProvider;
        AVAudioFormat m_AudioFormat;
        AVAudioEngine m_AudioEngine;
        AVAudioPlayerNode m_AudioPlayerNode;
        AVAudioPcmBuffer[] m_Buffers;
        SemaphoreSlim m_BufferSemaphore;
        Thread m_PlaybackThread;
        NSObject m_ConfigurationChangeNotificationToken;
        Exception m_ExternalException;
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
                if (m_AudioPlayerNode != null)
                {
                    m_AudioPlayerNode.Volume = m_Volume;
                }
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

        public WaveFormat OutputWaveFormat => throw new NotImplementedException();

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the player has stopped.
        /// </summary>
        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AVAudioEngineOut"/> class.
        /// </summary>
        public AVAudioEngineOut()
        {
            //Initialize the fields and properties
            m_WaveProvider = null;
            m_AudioFormat = null;
            m_AudioEngine = null;
            m_AudioPlayerNode = null;
            m_Buffers = null;
            m_BufferSemaphore = null;
            m_PlaybackThread = null;
            m_ConfigurationChangeNotificationToken = null;
            m_ExternalException = null;
            m_Volume = 1.0f;
            m_IsDisposed = false;
            PlaybackState = PlaybackState.Stopped;
            DesiredLatency = 300;
            NumberOfBuffers = 2;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the current instance of the <see cref="AVAudioEngineOut"/> class.
        /// </summary>
        ~AVAudioEngineOut()
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
            if (waveProvider == null)
            {
                throw new ArgumentNullException(nameof(waveProvider));
            }
            else if (waveProvider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat || waveProvider.WaveFormat.BitsPerSample != 32)
            {
                throw new ArgumentException("Input wave provider must be 32-bit IEEE float", nameof(waveProvider));
            }
            m_WaveProvider = waveProvider;

            //Initialize the audio engine
            m_AudioFormat = new AVAudioFormat(m_WaveProvider.WaveFormat.SampleRate, (uint)m_WaveProvider.WaveFormat.Channels);
            m_AudioPlayerNode = new AVAudioPlayerNode();
            m_AudioPlayerNode.Volume = Volume;
            m_AudioEngine = new AVAudioEngine();
            m_AudioEngine.AttachNode(m_AudioPlayerNode);
            m_AudioEngine.Connect(m_AudioPlayerNode, m_AudioEngine.MainMixerNode, m_AudioFormat);
            m_ConfigurationChangeNotificationToken = AVAudioEngine.Notifications.ObserveConfigurationChange(AVAudioEngine_ConfigurationChangeNotification);

            //Initialize the audio buffers
            m_Buffers = new AVAudioPcmBuffer[NumberOfBuffers];
            int bufferSize = m_WaveProvider.WaveFormat.ConvertLatencyToByteSize((DesiredLatency + NumberOfBuffers - 1) / NumberOfBuffers);
            bufferSize = (bufferSize + 3) & ~3;
            uint bufferFrames = (uint)(bufferSize / m_WaveProvider.WaveFormat.BlockAlign);
            for (int i = 0; i < NumberOfBuffers; i++)
            {
                m_Buffers[i] = new AVAudioPcmBuffer(m_AudioFormat, bufferFrames);
            }
            m_BufferSemaphore = new SemaphoreSlim(NumberOfBuffers, NumberOfBuffers);
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

            //Start the audio engine if necessary
            if (!m_AudioEngine.Running)
            {
                if (!m_AudioEngine.StartAndReturnError(out NSError error))
                {
                    throw new NSErrorException(error);
                }
            }

            //Start the wave player
            PlaybackState = PlaybackState.Playing;
            if (m_PlaybackThread == null || !m_PlaybackThread.IsAlive)
            {
                m_PlaybackThread = new Thread(PlaybackThread);
                m_PlaybackThread.Priority = ThreadPriority.Highest;
                m_PlaybackThread.Start();
            }
            m_AudioPlayerNode.Play();
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
            m_AudioPlayerNode.Pause();
            m_AudioEngine.Pause();
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
            m_PlaybackThread.Join();
            m_ExternalException = null;
        }

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="AVAudioEngineOut"/> class.
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
        /// Releases the unmanaged resources used by the <see cref="AVAudioEngineOut"/>, and optionally releases the managed resources.
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
                    m_AudioEngine?.Dispose();
                    m_AudioPlayerNode?.Dispose();
                    if (m_Buffers != null)
                    {
                        foreach (AVAudioPcmBuffer buffer in m_Buffers)
                        {
                            buffer.Dispose();
                        }
                    }
                    m_BufferSemaphore?.Dispose();
                    m_ConfigurationChangeNotificationToken?.Dispose();
                }
                m_IsDisposed = true;
            }
        }

        #endregion

        #region Private Methods

        private unsafe static void CopyBuffer(IWaveBuffer source, AVAudioPcmBuffer destination, WaveFormat waveFormat)
        {
            //Determine the number of frames to copy
            uint frames = (uint)(source.ByteBufferCount / waveFormat.BlockAlign);
            if (frames > destination.FrameCapacity)
            {
                frames = destination.FrameCapacity;
            }

            //Copy the specified wave buffer to the native PCM buffer
            if (waveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                if (waveFormat.BitsPerSample == 16)
                {
                    short** channelData = (short**)destination.Int16ChannelData;
                    for (int i = 0; i < waveFormat.Channels; i++)
                    {
                        for (int j = 0; j < frames; j++)
                        {
                            *(channelData[i] + j) = source.ShortBuffer[(waveFormat.Channels * j) + i];
                        }
                    }
                }
                else if (waveFormat.BitsPerSample == 32)
                {
                    int** channelData = (int**)destination.Int32ChannelData;
                    for (int i = 0; i < waveFormat.Channels; i++)
                    {
                        for (int j = 0; j < frames; j++)
                        {
                            *(channelData[i] + j) = source.IntBuffer[(waveFormat.Channels * j) + i];
                        }
                    }
                }
            }
            else if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                float** channelData = (float**)destination.FloatChannelData;
                for (int i = 0; i < waveFormat.Channels; i++)
                {
                    for (int j = 0; j < frames; j++)
                    {
                        *(channelData[i] + j) = source.FloatBuffer[(waveFormat.Channels * j) + i];
                    }
                }
            }
            destination.FrameLength = frames;
        }

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
                OnPlaybackStopped(m_ExternalException ?? exception);
            }
        }

        private void PlaybackLogic()
        {
            //Initialize the wave buffer
            int waveBufferSize = (int)m_Buffers[0].FrameCapacity * m_WaveProvider.WaveFormat.BlockAlign;
            WaveBuffer waveBuffer = new WaveBuffer(waveBufferSize);
            waveBuffer.ByteBufferCount = waveBufferSize;

            //Run the playback loop
            int bufferIndex = 0;
            while (PlaybackState != PlaybackState.Stopped)
            {
                //Wait for an available buffer
                if (!m_BufferSemaphore.Wait(DesiredLatency / NumberOfBuffers))
                {
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

                    //Copy the wave buffer to a native PCM buffer, and schedule it for playback
                    AVAudioPcmBuffer pcmBuffer = m_Buffers[bufferIndex];
                    CopyBuffer(waveBuffer, pcmBuffer, m_WaveProvider.WaveFormat);
                    m_AudioPlayerNode.ScheduleBuffer(pcmBuffer, () =>
                    {
                        m_BufferSemaphore.Release();
                    });

                    //Increment the buffer index
                    bufferIndex = (bufferIndex + 1) % m_Buffers.Length;
                }
                else
                {
                    //Wait for the scheduled buffers to empty
                    while (m_BufferSemaphore.CurrentCount < m_Buffers.Length - 1)
                    {
                        Thread.Sleep(10);
                    }
                    break;
                }
            }

            //Stop the audio player node and engine
            m_AudioPlayerNode.Stop();
            m_AudioEngine.Stop();

            //Reset the buffer semaphore
            if (m_BufferSemaphore.CurrentCount < m_Buffers.Length)
            {
                m_BufferSemaphore.Release(m_Buffers.Length - m_BufferSemaphore.CurrentCount);
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
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        #endregion

        #region Event Handlers

        private void AVAudioEngine_ConfigurationChangeNotification(object sender, NSNotificationEventArgs e)
        {
            //Reconfigure the audio engine after a configuration change
            m_AudioEngine.DisconnectNodeOutput(m_AudioPlayerNode);
            m_AudioEngine.Connect(m_AudioPlayerNode, m_AudioEngine.MainMixerNode, m_AudioFormat);

            //Restart the audio engine and player node if necessary
            if (PlaybackState != PlaybackState.Stopped)
            {
                try
                {
                    if (!m_AudioEngine.StartAndReturnError(out NSError error))
                    {
                        throw new NSErrorException(error);
                    }
                    if (PlaybackState == PlaybackState.Playing)
                    {
                        m_AudioPlayerNode.Play();
                    }
                }
                catch (Exception ex)
                {
                    m_ExternalException = ex;
                    Stop();
                }
            }
        }

        #endregion
    }

    //Credits https://gist.github.com/neilt6/287cc804a42b20f1973c9435ceec2686
}