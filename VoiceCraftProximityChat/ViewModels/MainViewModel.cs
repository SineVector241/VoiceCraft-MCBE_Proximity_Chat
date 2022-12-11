using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Collections.Generic;
using System.Windows.Input;
using VoiceCraftProximityChat.Models;
using VoiceCraftProximityChat.Utils;

namespace VoiceCraftProximityChat.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        //Fields
        private float _outputGain = 0f;
        private float _microphoneInput;
        private bool _isMuted;
        private bool _isDeafened;
        private string _title = "VoiceCraft - CONNECTED";
        private string _muteButtonContent = "Mute";
        private UdpClientModel udpClient = new UdpClientModel();
        private WaveIn input = new WaveIn();

        public float OutputGain { get => _outputGain; set { _outputGain = value; OnPropertyChanged(nameof(OutputGain)); } }
        public float MicrophoneInput { get => _microphoneInput; set { _microphoneInput = value; OnPropertyChanged(nameof(MicrophoneInput)); } }
        public bool IsMuted { get => _isMuted; set { _isMuted = value; OnPropertyChanged(nameof(IsMuted)); } }
        public bool IsDeafened { get => _isDeafened; set { _isDeafened = value; OnPropertyChanged(nameof(IsDeafened)); } }
        public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }
        public string MuteButtonContent { get => _muteButtonContent; set { _muteButtonContent = value; OnPropertyChanged(nameof(MuteButtonContent)); } }

        //Commands
        public ICommand MuteCommand { get; }
        public ICommand DeafenCommand { get; }

        //Constructor
        public MainViewModel()
        {
            MuteCommand = new DelegateCommand(ExecuteMuteCommand);
            DeafenCommand = new DelegateCommand(ExecuteDeafenCommand);

            //Audio Display Settings
            input.WaveFormat = G722ChatCodec.CodecInstance.RecordFormat;
            input.BufferMilliseconds = 50;
            input.DeviceNumber = 0;
            input.DataAvailable += SendAudio;
            input.StartRecording();

            if(UdpClientModel.IsConnected)
                udpClient.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.Ready, VCSessionKey = UdpClientModel._Key });
        }

        //Command Functions
        private void ExecuteMuteCommand(object obj)
        {
            IsMuted = !IsMuted;
            MuteButtonContent = IsMuted ? "Unmute" : "Mute";
        }

        private void ExecuteDeafenCommand(object obj)
        {
            IsDeafened = true;
        }

        //Sending Audio
        public void SendAudio(object? sender, WaveInEventArgs args)
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
            MicrophoneInput = max * 100;
            var encoded = G722ChatCodec.CodecInstance.Encode(args.Buffer, 0, args.BytesRecorded);

            if (UdpClientModel.IsConnected && !IsMuted && MicrophoneInput > 2)
                udpClient.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.AudioStream, VCSessionKey = UdpClientModel._Key, VCAudioBuffer = encoded });

            if (!UdpClientModel.IsConnected)
            {
                udpClient.Dispose();
                Title = "VoiceCraft - DISCONNECTED";
            }
        }
    }
}
