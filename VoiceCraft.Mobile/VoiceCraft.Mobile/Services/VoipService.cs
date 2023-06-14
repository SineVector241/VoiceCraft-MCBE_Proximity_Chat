using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Mobile.Audio;
using VoiceCraft.Mobile.Interfaces;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Network;
using VoiceCraft.Mobile.Network.Codecs;
using VoiceCraft.Mobile.Storage;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.Services
{
    public class VoipService
    {
        //State Variables
        private uint PacketCounter = 0;
        private bool Stopping = false;
        private bool IsMuted = false;
        private const int AudioFrameSizeMS = 40;

        private string StatusMessage = "Connecting...";
        private string Username = "";

        //VOIP and Audio handler variables
        private readonly NetworkManager VoipNetwork;
        private DateTime RecordDetection;
#nullable enable
        private readonly ServerModel? ServerInformation;
        private IWaveIn? AudioRecorder;
        private IWavePlayer? AudioPlayer;
        private MixingSampleProvider? Mixer;
        private SoftLimiter? Normalizer;
#nullable disable

        public VoipService()
        {
            ServerInformation = Database.GetPassableObject<ServerModel>();
            VoipNetwork = new NetworkManager(Database.GetSettings().DirectionalAudioEnabled, 
                ServerInformation.ClientSided, 
                (AudioCodecs)ServerInformation.Codec, 
                AudioFrameSizeMS);
        }

        public async Task Start(CancellationToken CT)
        {
            if (ServerInformation == null)
            {
                var message = new ServiceFailedMessage() { Message = "Cannot find server information!" };
                Device.BeginInvokeOnMainThread(() =>
                {
                    MessagingCenter.Send(message, "Error");
                });
                return;
            }

            await Task.Run(async () =>
            {
                //Event Initializations
                VoipNetwork.OnConnect += OnConnect;
                VoipNetwork.OnConnectError += OnConnectError;
                VoipNetwork.OnBinded += OnBinded;
                VoipNetwork.OnDisconnect += OnDisconnect;
                VoipNetwork.OnParticipantJoined += OnParticipantJoined;
                VoipNetwork.OnParticipantLeft += OnParticipantLeft;

                //Listen for UI Messages
                MessagingCenter.Subscribe<MuteUnmuteMessage>(this, "MuteUnmute", message =>
                {
                    IsMuted = !IsMuted;
                });

                RecordDetection = DateTime.UtcNow;

                try
                {
                    VoipNetwork.Connect(ServerInformation.IP, ServerInformation.Port);
                    while (!Stopping)
                    {
                        CT.ThrowIfCancellationRequested();
                        try
                        {
                            await Task.Delay(1000);
                            //Event Message Update
                            var message = new UpdateUIMessage()
                            {
                                Participants = VoipNetwork.Participants.Select(x => x.Value.Name).ToList(),
                                StatusMessage = StatusMessage,
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
                    VoipNetwork.OnConnect -= OnConnect;
                    VoipNetwork.OnConnectError -= OnConnectError;
                    VoipNetwork.OnBinded -= OnBinded;
                    VoipNetwork.OnDisconnect -= OnDisconnect;
                    VoipNetwork.OnParticipantJoined -= OnParticipantJoined;
                    VoipNetwork.OnParticipantLeft -= OnParticipantLeft;

                    if(AudioRecorder != null)
                        AudioRecorder.DataAvailable -= AudioDataAvailable;

                    AudioPlayer?.Dispose();
                    AudioRecorder?.Dispose();
                    Mixer?.RemoveAllMixerInputs();
                    Mixer = null;
                    Normalizer = null;
                    AudioRecorder = null;
                    AudioPlayer = null;

                    VoipNetwork.Disconnect(FireEvent: false);

                    MessagingCenter.Unsubscribe<MuteUnmuteMessage>(this, "MuteUnmute");
                }
            });
        }

        //Networking protocol executes in this order (most of the time).
        private void OnConnect(Network.Sockets.SocketTypes SocketType, int SampleRate, ushort Key)
        {
            //Register play and record formats after voice has connected...
            if(SocketType == Network.Sockets.SocketTypes.Voice && VoipNetwork.PlayFormat != null && VoipNetwork.RecordFormat != null)
            {
                IAudioManager audioManager = DependencyService.Get<IAudioManager>();

                Mixer = new MixingSampleProvider(VoipNetwork.PlayFormat) { ReadFully = true };
                Normalizer = new SoftLimiter(Mixer);
                Normalizer.Boost.CurrentValue = 10;

                AudioPlayer = audioManager.CreatePlayer(Normalizer);
                AudioRecorder = audioManager.CreateRecorder(VoipNetwork.RecordFormat);

                AudioRecorder.DataAvailable += AudioDataAvailable;

                StatusMessage = $"Connected - Key:{VoipNetwork.Key}\nWaiting for binding...";
                //Fire event message here.
                Device.BeginInvokeOnMainThread(() =>
                {
                    var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                    MessagingCenter.Send(message, "Update");
                });
            }
            else if (SocketType == Network.Sockets.SocketTypes.Signalling)
            {
                StatusMessage = $"Connecting Voice...\nPort: {VoipNetwork.VoicePort}";

                //Fire event message here.
                Device.BeginInvokeOnMainThread(() =>
                {
                    var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                    MessagingCenter.Send(message, "Update");
                });
            }
        }

        private void OnConnectError(Network.Sockets.SocketTypes SocketType, string reason)
        {
            Stopping = true;

            //Fire event message here
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new ServiceFailedMessage() { Message = reason };
                MessagingCenter.Send(message, "Error");
            });
        }

        private void OnBinded(string Username)
        {
            this.Username = Username;
            StatusMessage = $"Connected - Key: {VoipNetwork.Key}\n{Username}";

            //Last step of verification. We start sending data and playing any received data.
            AudioRecorder?.StartRecording();
            AudioPlayer?.Play();

            //Fire event message here.
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                MessagingCenter.Send(message, "Update");
            });
        }

        //Participant joining and leaving...
        private void OnParticipantJoined(ushort Key, VoiceCraftParticipant Participant)
        {
            Mixer?.AddMixerInput(Participant.AudioProvider);
        }

        private void OnParticipantLeft(ushort Key, VoiceCraftParticipant Participant)
        {
            Mixer?.RemoveMixerInput(Participant.AudioProvider);
        }

        //Event for on disconnections. If no reason DO NOT FIRE MESSAGE EVENT
        private void OnDisconnect(string reason = null)
        {
            Stopping = true;

            //We don't want to fire a failed message if there was no reason.
            if (reason == null)
                return;

            //Fire event message here
            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new ServiceFailedMessage() { Message = reason };
                MessagingCenter.Send(message, "Error");
            });
        }

        //Audio Events
        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (IsMuted)
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

            if (max > 0.08)
            {
                RecordDetection = DateTime.UtcNow;
            }

            if (DateTime.UtcNow.Subtract(RecordDetection).Seconds < 1)
            {
                //Prevent overload error.
                if (PacketCounter >= uint.MaxValue)
                    PacketCounter = 0;

                //Start counting up the packets sent. This is so packet loss can be detected
                PacketCounter++;

                VoipNetwork.SendAudio(e.Buffer, e.BytesRecorded, PacketCounter);
            }
            else
            {
                //Reset packet counter as soon as we stop sending audio.
                PacketCounter = 0;
            }
        }
    }
}
