using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using System.Collections.ObjectModel;
using System.Linq;
using VoiceCraft_Mobile.Audio;
using VoiceCraft_Mobile.Models;
using VoiceCraft_Mobile.Repositories;
using VoiceCraft_Mobile.Views;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using System;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class VoicePageViewModel : BaseViewModel
    {
        [ObservableProperty]
        string statusMessage = "Connecting...";

        [ObservableProperty]
        public ObservableCollection<ParticipantModel> participants = new ObservableCollection<ParticipantModel>();

        private AudioRecorder trackIn;

        public VoicePageViewModel()
        {
            Utils.OnPagePopped += OnPagePopped;

            Network.Network.Current.signallingClient.OnDisconnect += OnDisconnect;
            Network.Network.Current.signallingClient.OnConnect += OnConnect;
            Network.Network.Current.signallingClient.OnBinded += OnBinded;
            Network.Network.Current.signallingClient.OnParticipantLogin += OnParticipantLogin;
            Network.Network.Current.signallingClient.OnParticipantLogout += OnParticipantLogout;

            Network.Network.Current.voiceClient.OnConnect += VCConnected;
            Network.Network.Current.voiceClient.OnDisconnect += VCDisconnected;
            Network.Network.Current.voiceClient.OnAudioReceived += OnAudioReceived;
        }

        [RelayCommand]
        async void Disconnect()
        {
            Network.Network.Current.Disconnect();
            await Utils.GoToPreviousPageAsync();
        }

        [RelayCommand]
        void MuteUnmute()
        {
        }

        //Event Methods To Execute
        public void OnPagePopped(Page e)
        {
            if(e is VoicePage)
            {
                Network.Network.Current.signallingClient.OnDisconnect -= OnDisconnect;
                Network.Network.Current.signallingClient.OnConnect -= OnConnect;
                Network.Network.Current.signallingClient.OnBinded -= OnBinded;
                Network.Network.Current.signallingClient.OnParticipantLogin -= OnParticipantLogin;
                Network.Network.Current.signallingClient.OnParticipantLogout -= OnParticipantLogout;

                Network.Network.Current.voiceClient.OnConnect -= VCConnected;
                Network.Network.Current.voiceClient.OnDisconnect -= VCDisconnected;
                Network.Network.Current.voiceClient.OnAudioReceived -= OnAudioReceived;

                AudioPlayback.Current.ClearPlayer();
                trackIn.StopRecording();
                trackIn.Dispose();
            }
        }

        #region Signalling Network
        private void OnDisconnect(string reason)
        {
            if (reason != null)
                Utils.DisplayAlertAsync("Disconnect", reason);
            Utils.GoToPreviousPageAsync();
        }

        private void OnConnect(string key, string localServerId)
        {
            StatusMessage = $"Connecting Voice\nPort: {Network.Network.Current.signallingClient.VoicePort}";
            var server = Database.GetServers().FirstOrDefault(x => x.LocalId == localServerId);
            server.Id = key;

            Database.EditServer(server);

            for (var i = 0; i < Servers.Count; i++)
            {
                if (Servers[i].LocalId == server.LocalId)
                {
                    Servers[i] = server;
                    break;
                }
            }
            OnPropertyChanged(nameof(Servers));

            Network.Network.Current.voiceClient.Connect(Network.Network.Current.signallingClient.hostName, Network.Network.Current.signallingClient.VoicePort, key);
        }

        private void OnBinded(string name)
        {
            StatusMessage = $"Connected, Key: {Network.Network.Current.signallingClient.Key}\n Username: {name}";
        }

        private void OnParticipantLogin(ParticipantModel participant)
        {
            Participants.Add(participant);
            OnPropertyChanged(nameof(Participants));
            AudioPlayback.Current.AddMixerInput(participant.WaveProvider.ToSampleProvider());
        }

        private void OnParticipantLogout(string key)
        {
            var participant = Participants.IndexOf(x => x.LoginId == key);
            if (participant != -1)
            {
                AudioPlayback.Current.RemoveMixerInput(Participants[participant].WaveProvider.ToSampleProvider());
                Participants.RemoveAt(participant);
            }

            OnPropertyChanged(nameof(Participants));
        }
        #endregion

        #region Voice Network

        private void VCDisconnected(string reason)
        {
            if (reason != null)
                Utils.DisplayAlertAsync("Disconnect", reason);

            Network.Network.Current.signallingClient.Disconnect();
            Utils.GoToPreviousPageAsync();
        }

        private void VCConnected()
        {
            StatusMessage = $"Connected, Key: {Network.Network.Current.signallingClient.Key}\nWaiting for binding...";

            trackIn = new AudioRecorder();
            trackIn.BufferMilliseconds = 50;
            trackIn.WaveFormat = AudioPlayback.Current.recordFormat;
            trackIn.audioSource = Android.Media.AudioSource.VoiceCommunication;
            trackIn.NumberOfBuffers = 3;
            trackIn.DataAvailable += SendAudio;

            AudioPlayback.Current.Start();
            trackIn.StartRecording();
        }

        private void SendAudio(object? sender, WaveInEventArgs waveIn)
        {
            Network.Network.Current.voiceClient.Send(new VCVoice_Packet.VoicePacket() { 
                PacketDataIdentifier = VCVoice_Packet.PacketIdentifier.Audio,
                PacketAudio = waveIn.Buffer,
                PacketVersion = Network.Network.Version
            });
        }

        private void OnAudioReceived(byte[] Audio, string Key)
        {
            var participant = Participants.FirstOrDefault(x => x.LoginId == Key);
            if (participant != null)
                participant.WaveProvider.AddSamples(Audio, 0, Audio.Length);
        }
        #endregion
    }
}
