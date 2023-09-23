using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Client;
using VoiceCraft.Windows.Audio;
using VoiceCraft.Windows.Models;
using VoiceCraft.Windows.Storage;

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
        public delegate void UpdateStatus(UpdateStatusMessage message);
        public delegate void ParticipantsUpdate(UpdateParticipantsMessage message);
        public delegate void Disconnect(string? Reason);

        public event UpdateStatus? OnUpdateStatus;
        public event ParticipantsUpdate? OnParticipantsUpdate;
        public event Disconnect? OnServiceDisconnect;

        public VoipService()
        {
            var settings = Database.GetSettings();
            var server = Database.GetPassableObject<ServerModel>();
            var audioManager = new AudioManager();

            MicrophoneDetectionPercentage = settings.MicrophoneDetectionPercentage;

            IP = server.IP;
            Port = server.Port;

            Network = new VoiceCraftClient(server.Key, settings.ClientSidedPositioning ? Core.Packets.PositioningTypes.ClientSided : Core.Packets.PositioningTypes.ServerSided, App.Version, 40, settings.WebsocketPort)
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
                Network.OnDisconnected += OnDisconnected;

                AudioRecorder.DataAvailable += DataAvailable;

                try
                {
                    Network.Connect(IP, Port);
                    while (true)
                    {
                        CT.ThrowIfCancellationRequested();
                        try
                        {
                            await Task.Delay(200);
                            var message = new UpdateParticipantsMessage();
                            for(int i = 0; i < Network.Participants.Count; i++)
                            {
                                message.Participants = Network.Participants.Select(x => new ParticipantDisplayModel()
                                {
                                    IsDeafened = x.Value.Deafened,
                                    IsMuted = x.Value.Muted,
                                    IsSpeaking = DateTime.UtcNow.Subtract(RecordDetection).TotalSeconds < 1,
                                    Key = x.Key,
                                    Participant = x.Value
                                }).ToList();
                            }

                            App.Current?.Dispatcher.Invoke(() =>
                            {
                                OnParticipantsUpdate?.Invoke(message);
                            });
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Debug.WriteLine(ex);
#endif
                            App.Current?.Dispatcher.Invoke(() =>
                            {
                                var message = new ServiceErrorMessage() { Exception = ex };
                                OnServiceDisconnect?.Invoke(ex.Message);
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
                    Network.OnDisconnected -= OnDisconnected;

                    AudioRecorder.DataAvailable -= DataAvailable;

                    if (AudioPlayer.PlaybackState == PlaybackState.Playing)
                        AudioPlayer.Stop();

                    AudioRecorder.StopRecording();
                    AudioPlayer.Dispose();
                    AudioRecorder.Dispose();

                    Network.Disconnect();
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
                Network.SendAudio(e.Buffer, e.BytesRecorded);
            }
        }

        public ResponseUIMessage RequestUI()
        {
            var message = new ResponseUIMessage()
            {
                IsDeafened = Network.IsDeafened,
                IsMuted = Network.IsMuted,
                Participants = Network.Participants.Select(x => new ParticipantDisplayModel()
                {
                    IsDeafened = x.Value.Deafened,
                    IsMuted = x.Value.Muted,
                    IsSpeaking = DateTime.UtcNow.Subtract(RecordDetection).TotalSeconds < 1,
                    Key = x.Key,
                    Participant = x.Value
                }).ToList(),
                StatusMessage = StatusMessage
            };
            return message;
        }

        //Goes in this protocol order.
        private void OnConnected()
        {
            StatusMessage = Network.PositioningType == Core.Packets.PositioningTypes.ServerSided? $"Connected! Key - {Network.LoginKey}\nWaiting for binding..." : $"Connected! Key - {Network.LoginKey}\nWaiting for MCWSS connection...";

            App.Current?.Dispatcher.Invoke(() =>
            {
                var message = new UpdateStatusMessage() { StatusMessage = StatusMessage };
                OnUpdateStatus?.Invoke(message);
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
                var message = new UpdateStatusMessage() { StatusMessage = StatusMessage };
                OnUpdateStatus?.Invoke(message);
            });
        }

        private void Unbinded()
        {
            StatusMessage = $"Connected - Key: {Network.LoginKey}\nUnbinded. MCWSS Disconnected";

            App.Current?.Dispatcher.Invoke(() =>
            {
                var message = new UpdateStatusMessage() { StatusMessage = StatusMessage };
                OnUpdateStatus?.Invoke(message);
            });
        }

        private void OnDisconnected(string? Reason = null)
        {
            App.Current?.Dispatcher.Invoke(() =>
            {
                OnServiceDisconnect?.Invoke(Reason);
            });
        }
    }
}
