using NAudio.Wave;

namespace VoiceCraft.Maui.Interfaces
{
    public interface IAudioManager
    {
        /// <summary>
        /// Creates a recorder on the native device.
        /// </summary>
        /// <param name="AudioFormat"></param>
        /// <returns></returns>
        Task<IWaveIn> CreateRecorder(WaveFormat AudioFormat, int bufferMS);

        /// <summary>
        /// Creates a player on the native device.
        /// </summary>
        /// <param name="AudioFormat"></param>
        /// <returns></returns>
        Task<IWavePlayer> CreatePlayer(ISampleProvider AudioFormat);
    }
}
