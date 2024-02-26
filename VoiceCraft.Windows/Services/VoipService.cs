using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Client;
using VoiceCraft.Core.Audio;
using VoiceCraft.Windows.Audio;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Storage;
using System.Collections.Generic;
using System.Linq;

namespace VoiceCraft.Windows.Services
{
    public class VoipService
    {
        //State Variables
        private float MicrophoneDetectionPercentage;

        private string IP = string.Empty;
        private int Port = 9050;

        private string StatusMessage = "Connecting...";
        private string Username = "";

        //VOIP and Audio handler variables
        public VoiceCraftClient Network { get; private set; }
        private DateTime RecordDetection;
        private IWaveIn AudioRecorder;
        private IWavePlayer AudioPlayer;
        private SoftLimiter? Normalizer;

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

        public VoipService()
        {
            var settings = Database.GetSettings();
            var server = Database.GetPassableObject<ServerModel>();
            var audioManager = new AudioManager();

            MicrophoneDetectionPercentage = settings.MicrophoneDetectionPercentage;

            IP = server.IP;
            Port = server.Port;

            Network = new VoiceCraftClient(server.Key, settings.ClientSidedPositioning ? Core.Packets.PositioningTypes.ClientSided : Core.Packets.PositioningTypes.ServerSided, 20, settings.WebsocketPort)
            {
                LinearVolume = settings.LinearVolume,
                DirectionalHearing = settings.DirectionalAudioEnabled
            };

            if (settings.SoftLimiterEnabled)
            {
                Normalizer = new SoftLimiter(Network.Mixer);
                Normalizer.Boost.CurrentValue = settings.SoftLimiterGain;
                AudioPlayer = audioManager.CreatePlayer(Normalizer);
            }
            else
            {
                AudioPlayer = audioManager.CreatePlayer(Network.Mixer);
            }
            AudioRecorder = audioManager.CreateRecorder(Network.RecordFormat);
        }

        public async Task Start(CancellationToken CT)
        {
            await Task.Run(async () =>
            {
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
                    Network.Connect(IP, Port);

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
                                App.Current?.Dispatcher.Invoke(() =>
                                {
                                    OnSpeakingStatusChanged?.Invoke(currentSpeakingState);
                                });
                                previousSpeakingState = currentSpeakingState;
                            }

                            if (previousMuteState != Network.IsMuted)
                            {
                                App.Current?.Dispatcher.Invoke(() =>
                                {
                                    OnMutedStatusChanged?.Invoke(Network.IsMuted);
                                });
                                previousMuteState = Network.IsMuted;
                            }

                            if (previousDeafenedState != Network.IsDeafened)
                            {
                                App.Current?.Dispatcher.Invoke(() =>
                                {
                                    OnDeafenedStatusChanged?.Invoke(Network.IsDeafened);
                                });
                                previousDeafenedState = Network.IsDeafened;
                            }

                            var newTalkingParticipants = Network.Participants.Where(x => DateTime.UtcNow.Subtract(x.Value.LastSpoke).TotalSeconds < 1 && !talkingParticipants.Contains(x.Value));
                            foreach (var participant in newTalkingParticipants)
                            {
                                talkingParticipants.Add(participant.Value);
                                App.Current?.Dispatcher.Invoke(() =>
                                {
                                    OnParticipantSpeakingStatusChanged?.Invoke(participant.Value, true);
                                });
                            }

                            var oldTalkingParticipants = talkingParticipants.Where(x => DateTime.UtcNow.Subtract(x.LastSpoke).TotalMilliseconds >= 500).ToArray();
                            foreach (var participant in oldTalkingParticipants)
                            {
                                talkingParticipants.Remove(participant);
                                App.Current?.Dispatcher.Invoke(() =>
                                {
                                    OnParticipantSpeakingStatusChanged?.Invoke(participant, false);
                                });
                            }

                            if (AudioPlayer.PlaybackState != PlaybackState.Playing) AudioPlayer.Play();
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Debug.WriteLine(ex);
#endif
                            App.Current?.Dispatcher.Invoke(() =>
                            {
                                OnServiceDisconnected?.Invoke(ex.Message);
                            });
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

            if (max >= MicrophoneDetectionPercentage)
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
            AudioRecorder.StartRecording();
        }

        //Goes in this protocol order.
        private void OnConnected()
        {
            StatusMessage = Network.PositioningType == Core.Packets.PositioningTypes.ServerSided? $"Connected! Key - {Network.LoginKey}\nWaiting for binding..." : $"Connected! Key - {Network.LoginKey}\nWaiting for MCWSS connection...";

            App.Current?.Dispatcher.Invoke(() =>
            {
                OnStatusUpdated?.Invoke(StatusMessage);
            });
        }

        private void Binded(string? name)
        {
            Username = name ?? "<N.A.>";
            StatusMessage = $"Connected - Key: {Network.LoginKey}\n{Username}";

            //Last step of verification. We start sending data and playing any received data.
            try
            {
                AudioRecorder.StartRecording();
                AudioPlayer.Play();
            }
            catch { } //Do nothing. This is just to make sure that the recorder and player is working.

            App.Current?.Dispatcher.Invoke(() =>
            {
                OnStatusUpdated?.Invoke(StatusMessage);
            });
        }

        private void ChannelAdded(VoiceCraftChannel channel)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                OnChannelCreated?.Invoke(channel);
            });
        }

        private void ChannelJoined(VoiceCraftChannel channel)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                OnChannelEntered?.Invoke(channel);
            });
        }

        private void ChannelLeft(VoiceCraftChannel channel)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                OnChannelLeave?.Invoke(channel);
            });
        }

        private void ParticipantJoined(VoiceCraftParticipant participant, ushort key)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                OnParticipantAdded?.Invoke(participant);
            });
        }

        private void ParticipantLeft(VoiceCraftParticipant participant, ushort key)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                OnParticipantRemoved?.Invoke(participant);
            });
        }

        private void ParticipantUpdated(VoiceCraftParticipant participant, ushort key)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                OnParticipantChanged?.Invoke(participant);
            });
        }

        private void Unbinded()
        {
            StatusMessage = $"Connected - Key: {Network.LoginKey}\nUnbinded. MCWSS Disconnected";

            App.Current?.Dispatcher.Invoke(() =>
            {
                OnStatusUpdated?.Invoke(StatusMessage);
            });
        }

        private void OnDisconnected(string? Reason = null)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                OnServiceDisconnected?.Invoke(Reason);
            });
        }
    }
}
