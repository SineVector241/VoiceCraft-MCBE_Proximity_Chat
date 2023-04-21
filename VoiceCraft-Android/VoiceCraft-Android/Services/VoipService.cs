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
using VCVoice_Packet;

namespace VoiceCraft_Android.Services
{
    public class VoipService
    {
        private const int SampleRate = 16000;
        private const int Channels = 1;
        private bool Stopping = false;
        private bool IsMuted = false;
        private bool IsDeafened = false;
        private string Username = "";
        private string StatusMessage = "Connecting...";

        private List<ParticipantModel> Participants;
        private MixingSampleProvider Mixer;
        private IWaveIn AudioRecorder;
        private IWavePlayer AudioPlayer;
        private G722ChatCodec AudioCodec;

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
                AudioCodec = new G722ChatCodec();


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
                    IsMuted = !IsMuted;
                });

                MessagingCenter.Subscribe<DeafenUndeafenMessage>(this, "DeafenUndeafen", message => {
                    IsDeafened = !IsDeafened;
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
                                StatusMessage = StatusMessage,
                                IsDeafened = IsDeafened,
                                IsMuted = IsMuted
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
                    //Not sure if I explicitly need to do this but just to be safe I am doing it
                    SignalClient.OnConnect -= SC_OnConnect;
                    SignalClient.OnDisconnect -= SC_OnDisconnect;
                    SignalClient.OnBinded -= SC_OnBinded;
                    SignalClient.OnParticipantLogin -= SC_OnParticipantLogin;
                    SignalClient.OnParticipantLogout -= SC_OnParticipantLogout;

                    VCClient.OnConnect -= VC_OnConnect;
                    VCClient.OnDisconnect -= VC_OnDisconnect;
                    VCClient.OnAudioReceived -= VC_OnAudioReceived;

                    AudioRecorder.DataAvailable -= AudioDataAvailable;


                    AudioPlayer.Dispose();
                    AudioRecorder.Dispose();
                    AudioCodec.Dispose();
                    Mixer.RemoveAllMixerInputs();
                    Mixer = null;
                    AudioRecorder = null;
                    AudioPlayer = null;
                    AudioCodec = null;
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

                    MessagingCenter.Unsubscribe<MuteUnmuteMessage>(this, "MuteUnmute");

                    MessagingCenter.Unsubscribe<DeafenUndeafenMessage>(this, "DeafenUndeafen");
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

            //Last step of verification. We start sending data and playing any received data.
            AudioRecorder.StartRecording();
            AudioPlayer.Play();

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
            Mixer.AddMixerInput(participant.VolumeProvider);
            return Task.CompletedTask;
        }

        private Task SC_OnParticipantLogout(string key)
        {
            //Detect if its a participant logging out or the server requesting a disconnect
            if (!string.IsNullOrEmpty(key))
            {
                var participant = Participants.FirstOrDefault(x => x.LoginKey == key);
                if (participant != null)
                {
                    Mixer.RemoveMixerInput(participant.WaveProvider.ToSampleProvider());
                    Participants.Remove(participant);
                }
            }
            else
            {
                Stopping = true;
                //Fire event message here. Not necessarily an error. Its just to display that the server requested a disconnect.
                Device.BeginInvokeOnMainThread(() =>
                {
                    var message = new ServiceFailedMessage() { Message = "Server Requested Disconnect" };
                    MessagingCenter.Send(message, "Error");
                });
            }
            return Task.CompletedTask;
        }

        private Task SC_OnDisconnect(string reason)
        {
            Stopping = true;

            //We don't want to fire a failed message if there was no reason.
            if (reason == null)
                return Task.CompletedTask;

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
            StatusMessage = $"Connected - Key: {SignalClient.Key}\nWaiting For Binding...";

            //Fire event message here.
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                MessagingCenter.Send(message, "Update");
            });
            return Task.CompletedTask;
        }

        private Task VC_OnAudioReceived(byte[] Audio, string Key, float Volume)
        {
            var decoded = AudioCodec.Decode(Audio, 0, Audio.Length);

            var participant = Participants.FirstOrDefault(x => x.LoginKey == Key);
            if (participant != null)
            {
                participant.VolumeProvider.Volume = Volume;
                participant.WaveProvider.AddSamples(decoded, 0, decoded.Length);
            }

            return Task.CompletedTask;
        }

        private Task VC_OnDisconnect(string reason)
        {
            Stopping = true;

            //We don't want to fire a failed message if there was no reason.
            if (reason == null)
                return Task.CompletedTask;

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
            if(IsDeafened || IsMuted)
                return;

            var encoded = AudioCodec.Encode(e.Buffer, 0, e.BytesRecorded);

            var voicePacket = new VoicePacket()
            {
                PacketAudio = encoded,
                PacketDataIdentifier = PacketIdentifier.Audio,
                PacketVersion = Network.Network.Version
            };
            VCClient.Send(voicePacket);
        }
    }
}
