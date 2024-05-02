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
        IWaveIn CreateRecorder(WaveFormat AudioFormat, int bufferMS);

        /// <summary>
        /// Creates a player on the native device.
        /// </summary>
        /// <param name="AudioFormat"></param>
        /// <returns></returns>
        IWavePlayer CreatePlayer(ISampleProvider AudioFormat);

        /// <summary>
        /// Get's a list of input devices.
        /// </summary>
        /// <returns>The list of device names.</returns>
        string[] GetInputDevices();

        /// <summary>
        /// Get's a list of output devices.
        /// </summary>
        /// <returns>The list of device names.</returns>
        string[] GetOutputDevices();

        /// <summary>
        /// Get's the amount of available input audio devices.
        /// </summary>
        /// <returns>The number of available audio devices.</returns>
        int GetInputDeviceCount();

        /// <summary>
        /// Get's the amount of available output audio devices.
        /// </summary>
        /// <returns>The number of available audio devices.</returns>
        int GetOutputDeviceCount();

        /// <summary>
        /// Requests permissions to record.
        /// </summary>
        /// <returns></returns>
        Task<bool> RequestInputPermissions();
    }
}
