using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using VoiceCraftProximityChat.Models;
using VoiceCraftProximityChat.Network;
using VoiceCraftProximityChat.Utils;

namespace VoiceCraftProximityChat.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        //Fields
        private float _outputGain = 1.0f;
        private float _microphoneInput;
        private bool _isMuted;
        private bool _isDeafened;
        private string _title = "VoiceCraft - CONNECTED";
        private string _muteButtonContent = "Mute";
        private WaveIn input = new WaveIn();
        private ObservableCollection<Client> _clients = new ObservableCollection<Client>();

        public float OutputGain { get => _outputGain; set { _outputGain = value; OnPropertyChanged(nameof(OutputGain)); } }
        public float MicrophoneInput { get => _microphoneInput; set { _microphoneInput = value; OnPropertyChanged(nameof(MicrophoneInput)); } }
        public bool IsMuted { get => _isMuted; set { _isMuted = value; OnPropertyChanged(nameof(IsMuted)); } }
        public bool IsDeafened { get => _isDeafened; set { _isDeafened = value; OnPropertyChanged(nameof(IsDeafened)); } }
        public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }
        public string MuteButtonContent { get => _muteButtonContent; set { _muteButtonContent = value; OnPropertyChanged(nameof(MuteButtonContent)); } }
        public ObservableCollection<Client> Clients { get => _clients; set { _clients = value; OnPropertyChanged(nameof(Clients)); } }

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

            UdpNetworkHandler.Instance.Ready();
            UdpNetworkHandler.Instance.Logout += OnLogout;
            UdpNetworkHandler.Instance.ClientLogin += OnClientLogin;
            UdpNetworkHandler.Instance.ClientLogout += OnClientLogout;
        }

        private void OnClientLogout(object? sender, ClientLogoutEventArgs e)
        {
            App.Current.Dispatcher.Invoke(delegate {
                var client = Clients.FirstOrDefault(x => x.SessionKey == e.SessionKey );
                if (client != null) Clients.Remove(client);
            });
        }

        private void OnClientLogin(object? sender, ClientLoginEventArgs e)
        {
            App.Current.Dispatcher.Invoke(delegate { Clients.Add(new Client() { Username = e.Username, SessionKey = e.SessionKey }); });
        }

        private void OnLogout(object? sender, System.EventArgs e)
        {
            Title = "VoiceCraft - DISCONNECTED";
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

            if (UdpNetworkHandler.Instance._IsLoggedIn && !IsMuted && MicrophoneInput > 2)
                UdpNetwork.Instance.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.AudioStream, VCSessionKey = UdpNetworkHandler.Instance._Key, VCAudioBuffer = encoded });
        }
    }
}
