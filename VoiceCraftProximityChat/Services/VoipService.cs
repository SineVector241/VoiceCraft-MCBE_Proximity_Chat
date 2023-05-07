using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VCVoice_Packet;
using VoiceCraftProximityChat.Audio;
using VoiceCraftProximityChat.Models;
using VoiceCraftProximityChat.Network;
using VoiceCraftProximityChat.Storage;

namespace VoiceCraftProximityChat.Services
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
        private DateTime RecordDetection = DateTime.UtcNow;

        private List<ParticipantModel> Participants;
        private MixingSampleProvider Mixer;
        private SoftLimiter Normalizer;
        private IWaveIn AudioRecorder;
        private IWavePlayer AudioPlayer;

        private SignallingClient SignalClient;
        private VoiceClient VCClient;

        //Event Stuff
        public delegate Task Update(UpdateUIMessage message);
        public delegate Task Failed(ServiceFailedMessage message);

        public event Update OnUpdate;
        public event Failed OnFailed;

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
                    OnFailed?.Invoke(message);
                    return;
                }

                Participants = new List<ParticipantModel>();

                SignalClient = new SignallingClient();
                VCClient = new VoiceClient();

                var audioManager = new AudioManager();
                Mixer = new MixingSampleProvider(GetAudioFormat);
                Normalizer = new SoftLimiter(Mixer);
                Normalizer.Boost.CurrentValue = 5;

                AudioRecorder = audioManager.CreateRecorder(GetAudioFormat);
                AudioPlayer = audioManager.CreatePlayer(Normalizer);

                SignalClient.OnConnect += SC_OnConnect;
                SignalClient.OnDisconnect += SC_OnDisconnect;
                SignalClient.OnBinded += SC_OnBinded;
                SignalClient.OnParticipantLogin += SC_OnParticipantLogin;
                SignalClient.OnParticipantLogout += SC_OnParticipantLogout;

                VCClient.OnConnect += VC_OnConnect;
                VCClient.OnDisconnect += VC_OnDisconnect;
                VCClient.OnAudioReceived += VC_OnAudioReceived;

                AudioRecorder.DataAvailable += AudioDataAvailable;

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
                            OnUpdate?.Invoke(message);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
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
                    Mixer.RemoveAllMixerInputs();
                    Mixer = null;
                    Normalizer = null;
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
            var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
            OnUpdate?.Invoke(message);
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
            var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
            OnUpdate?.Invoke(message);
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
                var message = new ServiceFailedMessage() { Message = "Server Requested Disconnect" };
                OnFailed?.Invoke(message);
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
            var message = new ServiceFailedMessage() { Message = reason };
            OnFailed?.Invoke(message);
            return Task.CompletedTask;
        }
        #endregion

        //Goes in this verification flow format most of the time.
        #region Voice Client Events
        private Task VC_OnConnect()
        {
            StatusMessage = $"Connected - Key: {SignalClient.Key}\nWaiting For Binding...";

            //Fire event message here.
            var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
            OnUpdate?.Invoke(message);
            return Task.CompletedTask;
        }

        private Task VC_OnAudioReceived(byte[] Audio, string Key, float Volume, int BytesRecorded)
        {
            _ = Task.Factory.StartNew(() =>
            {
                var participant = Participants.FirstOrDefault(x => x.LoginKey == Key);
                if (participant != null)
                {
                    participant.VolumeProvider.Volume = Volume;
                    participant.WaveProvider.AddSamples(Audio, 0, BytesRecorded);
                }
            });

            if(!Stopping && AudioPlayer.PlaybackState == PlaybackState.Stopped)
                AudioPlayer.Play();

            return Task.CompletedTask;
        }

        private Task VC_OnDisconnect(string reason)
        {
            Stopping = true;

            //We don't want to fire a failed message if there was no reason.
            if (reason == null)
                return Task.CompletedTask;

            //Fire event message here
            var message = new ServiceFailedMessage() { Message = reason };
            OnFailed?.Invoke(message);
            return Task.CompletedTask;
        }
        #endregion

        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (IsDeafened || IsMuted)
                return;

            var buffer = new WaveBuffer(e.Buffer);

            float max = 0;
            // interpret as 32 bit audio
            for (int index = 0; index < e.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];

                // absolute value 
                if (sample < 0) sample = -sample;
                // is this the max value?
                if (sample > max) max = sample;
            }

            if (max > 0.08)
            {
                RecordDetection = DateTime.UtcNow;
            }

            if (DateTime.UtcNow.Subtract(RecordDetection).Seconds < 1)
            {
                var voicePacket = new VoicePacket()
                {
                    PacketAudio = e.Buffer,
                    PacketDataIdentifier = PacketIdentifier.Audio,
                    PacketVersion = Network.Network.Version,
                    PacketBytesRecorded = e.BytesRecorded
                };
                VCClient.Send(voicePacket);
            }
        }

        public void MuteUnmute(bool value)
        {
            IsMuted = value;
        }
    }
}
