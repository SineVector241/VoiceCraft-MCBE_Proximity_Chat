using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Windows.Input;
using VoiceCraftProximityChat.Models;

namespace VoiceCraftProximityChat.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        //Fields
        private float _microphoneInput;
        private bool _isMuted;
        private bool _isDeafened;
        private string _title = "VoiceCraft - CONNECTED";
        private UdpClientModel udpClient = new UdpClientModel();
        private WaveIn input = new WaveIn();

        public float MicrophoneInput { get => _microphoneInput; set { _microphoneInput = value; OnPropertyChanged(nameof(MicrophoneInput)); } }
        public bool IsMuted { get => _isMuted; set { _isMuted = value; OnPropertyChanged(nameof(IsMuted)); } }
        public bool IsDeafened { get => _isDeafened; set { _isDeafened = value; OnPropertyChanged(nameof(IsDeafened)); } }
        public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }

        //Commands
        public ICommand MuteCommand { get; }
        public ICommand DeafenCommand { get; }

        //Constructor
        public MainViewModel()
        {
            MuteCommand = new DelegateCommand(ExecuteMuteCommand);
            DeafenCommand = new DelegateCommand(ExecuteDeafenCommand);

            //Audio Display Settings
            input.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(16000,1);
            input.BufferMilliseconds = 50;
            input.DeviceNumber = 0;
            input.DataAvailable += SendAudio;
            input.StartRecording();

            udpClient.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ready, VCSessionKey = UdpClientModel._Key });
        }

        //Command Functions
        private void ExecuteMuteCommand(object obj)
        {
            IsMuted = true;
        }

        private void ExecuteDeafenCommand(object obj)
        {
            IsDeafened = true;
        }

        //Sending Audio
        public void SendAudio(object? sender, WaveInEventArgs args)
        {
            if (UdpClientModel.IsConnected && !IsMuted)
                udpClient.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.AudioStream, VCSessionKey = UdpClientModel._Key, VCAudioBuffer = args.Buffer });

            if (!UdpClientModel.IsConnected)
            {
                udpClient.Dispose();
                Title = "VoiceCraft - DISCONNECTED";
            }
        }
    }
}
