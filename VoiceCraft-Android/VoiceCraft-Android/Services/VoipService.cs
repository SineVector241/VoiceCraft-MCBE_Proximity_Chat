using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft_Android.Interfaces;
using VoiceCraft_Android.Models;
using VoiceCraft_Android.Storage;
using VoiceCraft_Android.Network;
using Xamarin.Forms;

namespace VoiceCraft_Android.Services
{
    public class VoipService
    {
        private const int SampleRate = 64000;
        private const int Channels = 1;
        private bool Stopping = false;
        private string Username = "";
        private string StatusMessage = "";

        private List<ParticipantModel> Participants;
        private MixingSampleProvider Mixer;
        private IWaveIn AudioRecorder;
        private IWavePlayer AudioPlayer;

        private SignallingClient SignalClient;
        private VoiceClient VCClient;

        public static WaveFormat GetRecordFormat { get => new WaveFormat(SampleRate, Channels); }
        public static WaveFormat GetAudioFormat { get => WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels); }

        public async Task Run(CancellationToken ct, string serverName)
        {
            await Task.Run(async () =>
            {
                //Get server information first
                var server = Database.GetServerByName(serverName);
                if (server == null)
                {
                    var message = new ServiceFailedMessage() { Message = "Cannot find server information!" };
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        MessagingCenter.Send(message, "Update");
                    });
                    return;
                }

                Participants = new List<ParticipantModel>();

                SignalClient = new SignallingClient();
                VCClient = new VoiceClient();

                IAudioManager audioManager = DependencyService.Get<IAudioManager>();
                Mixer = new MixingSampleProvider(GetAudioFormat) { ReadFully = true };
                AudioRecorder = audioManager.CreateRecorder(GetRecordFormat);
                AudioPlayer = audioManager.CreatePlayer(Mixer);


                SignalClient.OnConnect += SC_OnConnect;
                SignalClient.OnDisconnect += SC_OnDisconnect;
                SignalClient.OnBinded += SC_OnBinded;
                SignalClient.OnParticipantLogin += SC_OnParticipantLogin;
                SignalClient.OnParticipantLogout += SC_OnParticipantLogout;

                VCClient.OnConnect += VC_OnConnect;
                VCClient.OnDisconnect += VC_OnDisconnect;
                VCClient.OnAudioReceived += VC_OnAudioReceived;

                AudioRecorder.DataAvailable += AudioDataAvailable;

                MessagingCenter.Subscribe<MuteUnmuteMessage>(this, "MuteUnmute", message => {
                    var p = new ParticipantModel()
                    {
                        LoginKey = "AAAAA",
                        Name = "Dummy",
                        WaveProvider = new BufferedWaveProvider(GetRecordFormat)
                    };

                    Mixer.AddMixerInput(p.WaveProvider);
                    Participants.Add(p);
                });

                //Connection/Verification starts right at this point.
                SignalClient.Connect(server.IP, server.Port, server.LoginKey, serverName);
                try
                {
                    while (!Stopping)
                    {
                        ct.ThrowIfCancellationRequested();
                        try
                        {
                            await Task.Delay(1000);
                            //Event Message Update
                            var message = new UpdateUIMessage()
                            {
                                Participants = Participants,
                                StatusMessage = StatusMessage
                            };
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                MessagingCenter.Send(message, "Update");
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                var message = new ServiceErrorMessage() { Exception = ex };
                                MessagingCenter.Send(message, "Error");
                            });
                        }
                    }
                }
                catch (OperationCanceledException)
                { }
                finally
                {
                    AudioPlayer.Dispose();
                    AudioRecorder.Dispose();
                    Mixer.RemoveAllMixerInputs();
                    Mixer = null;
                    AudioRecorder = null;
                    AudioPlayer = null;
                    Participants.Clear();
                    Participants = null;
                    try
                    {
                        SignalClient.Disconnect();
                        SignalClient = null;
                    }
                    catch
                    { }

                    try
                    {
                        VCClient.Disconnect();
                        VCClient = null;
                    }
                    catch
                    { }
                }
            });
        }

        //Verification Flow does go in this order most of the time.
        #region Signal Client Events
        private Task SC_OnConnect(string key, string serverName)
        {
            VCClient.Connect(SignalClient.hostName, SignalClient.VoicePort, SignalClient.Key);
            StatusMessage = $"Voice Connecting\nPort: {SignalClient.VoicePort}";

            var server = Database.GetServerByName(serverName);
            server.LoginKey = key;
            Database.EditServer(server);

            //Fire message event here
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                MessagingCenter.Send(message, "Update");
            });
            return Task.CompletedTask;
        }

        private Task SC_OnBinded(string name)
        {
            Username = name;
            StatusMessage = $"Connected - Key: {SignalClient.Key}\n{Username}";

            //Fire event message here.
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                MessagingCenter.Send(message, "Update");
            });
            return Task.CompletedTask;
        }

        private Task SC_OnParticipantLogin(ParticipantModel participant)
        {
            Participants.Add(participant);
            Mixer.AddMixerInput(participant.WaveProvider);
            return Task.CompletedTask;
        }

        private Task SC_OnParticipantLogout(string key)
        {
            var participant = Participants.FirstOrDefault(x => x.LoginKey == key);
            if (participant != null)
            {
                Mixer.RemoveMixerInput(participant.WaveProvider.ToSampleProvider());
                Participants.Remove(participant);
            }
            return Task.CompletedTask;
        }

        private Task SC_OnDisconnect(string reason)
        {
            //We don't want to fire a failed message if there was no reason.
            if (reason == null)
                return Task.CompletedTask;

            Stopping = true;

            //Fire event message here
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new ServiceFailedMessage() { Message = reason };
                MessagingCenter.Send(message, "Error");
            });
            return Task.CompletedTask;
        }
        #endregion

        //Goes in this verification flow format most of the time.
        #region Voice Client Events
        private Task VC_OnConnect()
        {
            AudioRecorder.StartRecording();
            AudioPlayer.Play();
            StatusMessage = $"Connected - Key: {SignalClient.Key}\nWaiting For Binding...";

            //Fire event message here.
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                MessagingCenter.Send(message, "Update");
            });
            return Task.CompletedTask;
        }

        private Task VC_OnAudioReceived(byte[] Audio, string Key)
        {
            var participant = Participants.FirstOrDefault(x => x.LoginKey == Key);
            if (participant != null)
                participant.WaveProvider.AddSamples(Audio, 0, Audio.Length);

            return Task.CompletedTask;
        }

        private Task VC_OnDisconnect(string reason)
        {
            //We don't want to fire a failed message if there was no reason.
            if (reason == null)
                return Task.CompletedTask;


            Stopping = true;

            //Fire event message here
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new ServiceFailedMessage() { Message = reason };
                MessagingCenter.Send(message, "Error");
            });
            return Task.CompletedTask;
        }
        #endregion

        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
        }
    }
}
