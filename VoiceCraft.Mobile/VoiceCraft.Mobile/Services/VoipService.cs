using NAudio.Wave;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Mobile.Audio;
using VoiceCraft.Mobile.Interfaces;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Network;
using VoiceCraft.Mobile.Storage;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.Services
{
    public class VoipService
    {
        //State Variables
        private bool IsMuted = false;
        public bool SendDisconnectPacket = false;

        private string StatusMessage = "Connecting...";
        private string Username = "";

        //VOIP and Audio handler variables
        private NetworkManager Network;
        private DateTime RecordDetection;
        private IWaveIn AudioRecorder;
        private IWavePlayer AudioPlayer;
        private SoftLimiter Normalizer;

        //Events
        public delegate void Update(UpdateUIMessage Data);
        public delegate void Disconnect(string? Reason);

        public event Update? OnUpdate;
        public event Disconnect? OnServiceDisconnect;

        public VoipService()
        {
            var settings = Database.GetSettings();
            var server = Database.GetPassableObject<ServerModel>();
            var audioManager = DependencyService.Get<IAudioManager>();

            Network = new NetworkManager(server.IP, server.Port, server.Key, server.ClientSided, settings.DirectionalAudioEnabled);
            RecordDetection = DateTime.UtcNow;
            Normalizer = new SoftLimiter(Network.Mixer);
            Normalizer.Boost.CurrentValue = 5;
            AudioRecorder = audioManager.CreateRecorder(Network.RecordFormat);
            AudioPlayer = audioManager.CreatePlayer(Normalizer);
        }

        public async Task Start(CancellationToken CT)
        {
            await Task.Run(async () =>
            {
                //Event Initializations
                Network.OnSignallingConnect += SC_OnConnect;
                Network.OnVoiceConnect += VC_OnConnect;
                Network.OnWebsocketConnect += WS_OnConnect;
                Network.OnWebsocketDisconnect += WS_OnDisconnect;
                Network.OnBind += OnBind;
                Network.OnDisconnect += OnDisconnect;

                AudioRecorder.DataAvailable += DataAvailable;

                RecordDetection = DateTime.UtcNow;

                try
                {
                    Network.StartConnect();
                    while (true)
                    {
                        CT.ThrowIfCancellationRequested();
                        try
                        {
                            await Task.Delay(1000);
                            //Event Message Update
                            var message = new UpdateUIMessage()
                            {
                                Participants = Network.Participants.Select(x => x.Value.Name).ToList(),
                                StatusMessage = StatusMessage,
                                IsMuted = IsMuted
                            };
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                OnUpdate?.Invoke(message);
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            Device.BeginInvokeOnMainThread(() =>
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
                    Network.OnSignallingConnect -= SC_OnConnect;
                    Network.OnVoiceConnect -= VC_OnConnect;
                    Network.OnWebsocketConnect -= WS_OnConnect;
                    Network.OnWebsocketDisconnect -= WS_OnDisconnect;
                    Network.OnBind -= OnBind;
                    Network.OnDisconnect -= OnDisconnect;

                    AudioRecorder.DataAvailable -= DataAvailable;

                    if (AudioPlayer.PlaybackState == PlaybackState.Playing)
                        AudioPlayer.Stop();

                    AudioPlayer.Dispose();
                    AudioRecorder.Dispose();

                    Network.StartDisconnect(SendDisconnectPacket: SendDisconnectPacket);
                }
            });
        }

        public void MuteUnmute()
        {
            IsMuted = !IsMuted;
        }

        //Audio Events
        private void DataAvailable(object? sender, WaveInEventArgs e)
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
                Network.SendAudio(e.Buffer, e.BytesRecorded);
            }
            else
            {
                //Reset packet counter as soon as we stop sending audio.
                Network.ResetPacketCounter();
            }
        }

        //Goes in this protocol order.
        private void SC_OnConnect(ushort Key, int VoicePort)
        {
            StatusMessage = $"Connecting Voice...\nPort: {Network.VoicePort}";

            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                OnUpdate?.Invoke(message);
            });
        }

        private void VC_OnConnect()
        {
            StatusMessage = Network.ClientSided ? "Voice Connected\nWaiting for MCWSS Connection..." : $"Connected - Key:{Network.Key}\nWaiting for binding...";

            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                OnUpdate?.Invoke(message);
            });
        }

        private void WS_OnConnect()
        {
            StatusMessage = "Connected\n<Username>";

            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                OnUpdate?.Invoke(message);
            });
        }

        private void WS_OnDisconnect()
        {
            StatusMessage = "MCWSS Disconnected!\nWaiting for reconnection...";

            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                OnUpdate?.Invoke(message);
            });
        }

        private void OnBind(string Name)
        {
            Username = Name;
            StatusMessage = $"Connected - Key: {Network.Key}\n{Username}";

            //Last step of verification. We start sending data and playing any received data.
            AudioRecorder.StartRecording();
            AudioPlayer.Play();

            Device.BeginInvokeOnMainThread(() =>
            {
                var message = new UpdateUIMessage() { StatusMessage = StatusMessage };
                OnUpdate?.Invoke(message);
            });
        }

        private void OnDisconnect(string? Reason = null)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                OnServiceDisconnect?.Invoke(Reason);
            });
        }
    }
}
