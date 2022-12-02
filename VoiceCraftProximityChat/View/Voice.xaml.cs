using VoiceCraftProximityChat.Core.Network;
using System.Windows;
using System.Windows.Input;
using NAudio.Wave;
using System.Threading;

namespace VoiceCraftProximityChat.View
{
    /// <summary>
    /// Interaction logic for Voice.xaml
    /// </summary>
    public partial class VoiceWindow : Window
    {
        private WaveIn waveIn = new WaveIn();
        public VoiceWindow()
        {
            Audio.Init();
            Network.SendReadyEvent();
            InitializeComponent();
            waveIn.BufferMilliseconds = 50;
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new WaveFormat(16000, 1);
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.StartRecording();
            Network.timer = new Timer(Network.lastPingCheck, null, 0, 2000);
        }

        private void WaveIn_DataAvailable(object? sender, WaveInEventArgs args)
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
            MicrophoneLevel.Value = max * 100;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
