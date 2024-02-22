using NAudio.Wave;
using VoiceCraft.Core.Client;
using VoiceCraft.Core.Audio;
using VoiceCraft.Maui.Models;
using System.Diagnostics;

namespace VoiceCraft.Maui.Services
{
    public class VoipService
    {
        //State Variables
        public string StatusMessage { get; private set; } = "Connecting...";
        private string Username = "";

        //VOIP and Audio handler variables
        public VoiceCraftClient Network { get; private set; }
        private DateTime RecordDetection;
        private IWaveIn? AudioRecorder;
        private IWavePlayer? AudioPlayer;
        private SoftLimiter? Normalizer;
        private SettingsModel Settings;
        private ServerModel Server;

        //Events
        public delegate void StatusMessageUpdated(string status);
        public delegate void SpeakingStatusChanged(bool status);
        public delegate void MutedStatusChanged(bool status);
        public delegate void DeafenedStatusChanged(bool status);
        public delegate void ParticipantAdded(VoiceCraftParticipant participant);
        public delegate void ParticipantRemoved(VoiceCraftParticipant participant);
        public delegate void ParticipantChanged(VoiceCraftParticipant participant);
        public delegate void ParticipantSpeakingStatusChanged(VoiceCraftParticipant participant, bool status);
        public delegate void ChannelCreated(VoiceCraftChannel channel);
        public delegate void ChannelEntered(VoiceCraftChannel channel);
        public delegate void ChannelLeave(VoiceCraftChannel channel);
        public delegate void Disconnected(string? Reason);

        public event StatusMessageUpdated? OnStatusUpdated;
        public event SpeakingStatusChanged? OnSpeakingStatusChanged;
        public event MutedStatusChanged? OnMutedStatusChanged;
        public event DeafenedStatusChanged? OnDeafenedStatusChanged;
        public event ParticipantAdded? OnParticipantAdded;
        public event ParticipantRemoved? OnParticipantRemoved;
        public event ParticipantChanged? OnParticipantChanged;
        public event ParticipantSpeakingStatusChanged? OnParticipantSpeakingStatusChanged;
        public event ChannelCreated? OnChannelCreated;
        public event ChannelEntered? OnChannelEntered;
        public event ChannelLeave? OnChannelLeave;
        public event Disconnected? OnServiceDisconnected;

        public VoipService(ServerModel server)
        {
            Settings = Database.Instance.Settings;
            Server = server;

            Network = new VoiceCraftClient(server.Key, Settings.ClientSidedPositioning ? Core.Packets.PositioningTypes.ClientSided : Core.Packets.PositioningTypes.ServerSided, 40, Settings.WebsocketPort)
            {
                LinearVolume = Settings.LinearVolume,
                DirectionalHearing = Settings.DirectionalAudioEnabled
            };
        }

        public async Task Start(CancellationToken CT)
        {
            await Task.Run(async () =>
            {
                var audioManager = new AudioManager();

                if (Settings.SoftLimiterEnabled)
                {
                    Normalizer = new SoftLimiter(Network.Mixer);
                    Normalizer.Boost.CurrentValue = Settings.SoftLimiterGain;
                    AudioPlayer = await audioManager.CreatePlayer(Normalizer);
                }
                else
                {
                    AudioPlayer = await audioManager.CreatePlayer(Network.Mixer);
                }
                AudioRecorder = await audioManager.CreateRecorder(Network.RecordFormat);

                //Event Initializations
                Network.OnConnected += OnConnected;
                Network.OnBinded += Binded;
                Network.OnUnbinded += Unbinded;
                Network.OnParticipantJoined += ParticipantJoined;
                Network.OnParticipantLeft += ParticipantLeft;
                Network.OnParticipantUpdated += ParticipantUpdated;
                Network.OnChannelAdded += ChannelAdded;
                Network.OnChannelJoined += ChannelJoined;
                Network.OnChannelLeft += ChannelLeft;
                Network.OnDisconnected += OnDisconnected;

                AudioRecorder.DataAvailable += DataAvailable;
                AudioRecorder.RecordingStopped += RecordingStopped;

                try
                {
                    Network.Connect(Server.IP, Server.Port);

                    //Setup state variables for this instance.
                    List<VoiceCraftParticipant> talkingParticipants = new List<VoiceCraftParticipant>();
                    bool previousSpeakingState = false;
                    bool previousMuteState = false;
                    bool previousDeafenedState = false;

                    while (true)
                    {
                        CT.ThrowIfCancellationRequested();
                        try
                        {
                            await Task.Delay(200);
                            var currentSpeakingState = DateTime.UtcNow.Subtract(RecordDetection).TotalMilliseconds < 500;
                            if (previousSpeakingState != currentSpeakingState)
                            {
                                OnSpeakingStatusChanged?.Invoke(currentSpeakingState);
                                previousSpeakingState = currentSpeakingState;
                            }

                            if (previousMuteState != Network.IsMuted)
                            {
                                OnMutedStatusChanged?.Invoke(Network.IsMuted);
                                previousMuteState = Network.IsMuted;
                            }

                            if (previousDeafenedState != Network.IsDeafened)
                            {
                                OnDeafenedStatusChanged?.Invoke(Network.IsDeafened);
                                previousDeafenedState = Network.IsDeafened;
                            }

                            var newTalkingParticipants = Network.Participants.Where(x => DateTime.UtcNow.Subtract(x.Value.LastSpoke).TotalSeconds < 1 && !talkingParticipants.Contains(x.Value));
                            foreach (var participant in newTalkingParticipants)
                            {
                                talkingParticipants.Add(participant.Value);
                                OnParticipantSpeakingStatusChanged?.Invoke(participant.Value, true);
                            }

                            var oldTalkingParticipants = talkingParticipants.Where(x => DateTime.UtcNow.Subtract(x.LastSpoke).TotalMilliseconds >= 500).ToArray();
                            foreach (var participant in oldTalkingParticipants)
                            {
                                talkingParticipants.Remove(participant);
                                OnParticipantSpeakingStatusChanged?.Invoke(participant, false);
                            }

                            if (AudioPlayer.PlaybackState != PlaybackState.Playing) AudioPlayer.Play();
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Debug.WriteLine(ex);
#endif
                            OnServiceDisconnected?.Invoke(ex.Message);
                        }
                    }
                }
                catch (OperationCanceledException)
                { }
                finally
                {
                    Network.OnConnected -= OnConnected;
                    Network.OnBinded -= Binded;
                    Network.OnUnbinded -= Unbinded;
                    Network.OnParticipantJoined -= ParticipantJoined;
                    Network.OnParticipantLeft -= ParticipantLeft;
                    Network.OnParticipantUpdated -= ParticipantUpdated;
                    Network.OnChannelAdded -= ChannelAdded;
                    Network.OnChannelJoined -= ChannelJoined;
                    Network.OnChannelLeft -= ChannelLeft;
                    Network.OnDisconnected -= OnDisconnected;

                    AudioRecorder.DataAvailable -= DataAvailable;
                    AudioRecorder.RecordingStopped -= RecordingStopped;

                    if (AudioPlayer.PlaybackState == PlaybackState.Playing)
                        AudioPlayer.Stop();

                    AudioRecorder.StopRecording();
                    AudioPlayer.Dispose();
                    AudioRecorder.Dispose();

                    Network.Disconnect();
                    Network.Dispose();
                }
            });
        }

        //Audio Events
        private void DataAvailable(object? sender, WaveInEventArgs e)
        {
            if (Network.IsMuted || Network.IsDeafened)
                return;

            float max = 0;
            // interpret as 16 bit audio
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                // to floating point
                var sample32 = sample / 32768f;
                // absolute value 
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            if (max >= Settings.MicrophoneDetectionPercentage)
            {
                RecordDetection = DateTime.UtcNow;
            }

            if (DateTime.UtcNow.Subtract(RecordDetection).TotalSeconds < 1)
            {
                _ = Task.Run(() => Network.SendAudio(e.Buffer, e.BytesRecorded));
            }
        }

        private void RecordingStopped(object? sender, StoppedEventArgs e)
        {
            AudioRecorder?.StartRecording();
        }

        //Goes in this protocol order.
        private void OnConnected()
        {
            StatusMessage = Network.PositioningType == Core.Packets.PositioningTypes.ServerSided ? $"Connected! Key - {Network.LoginKey}\nWaiting for binding..." : $"Connected! Key - {Network.LoginKey}\nWaiting for MCWSS connection...";
            OnStatusUpdated?.Invoke(StatusMessage);
        }

        private void Binded(string? name)
        {
            Username = name ?? "<N.A.>";
            StatusMessage = $"Connected - Key: {Network.LoginKey}\n{Username}";

            //Last step of verification. We start sending data and playing any received data.
            try
            {
                AudioRecorder?.StartRecording();
                AudioPlayer?.Play();
            }
            catch { } //Do nothing. This is just to make sure that the recorder and player is working.
            OnStatusUpdated?.Invoke(StatusMessage);
        }

        private void ChannelAdded(VoiceCraftChannel channel)
        {
            OnChannelCreated?.Invoke(channel);
        }

        private void ChannelJoined(VoiceCraftChannel channel)
        {
            OnChannelEntered?.Invoke(channel);
        }

        private void ChannelLeft(VoiceCraftChannel channel)
        {
            OnChannelLeave?.Invoke(channel);
        }

        private void ParticipantJoined(VoiceCraftParticipant participant, ushort key)
        {
            OnParticipantAdded?.Invoke(participant);
        }

        private void ParticipantLeft(VoiceCraftParticipant participant, ushort key)
        {
            OnParticipantRemoved?.Invoke(participant);
        }

        private void ParticipantUpdated(VoiceCraftParticipant participant, ushort key)
        {
            OnParticipantChanged?.Invoke(participant);
        }

        private void Unbinded()
        {
            StatusMessage = $"Connected - Key: {Network.LoginKey}\nUnbinded. MCWSS Disconnected";
            OnStatusUpdated?.Invoke(StatusMessage);
        }

        private void OnDisconnected(string? Reason = null)
        {
            OnServiceDisconnected?.Invoke(Reason);
        }
    }
}
