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
using Concentus.Structs;
using VoiceCraft_Android.Audio;

namespace VoiceCraft_Android.Services
{
    public class VoipService
    {
        private const int SampleRate = 48000;
        private const int Channels = 1;
        private int PacketCounter = 0;
        private bool Stopping = false;
        private bool IsMuted = false;
        private bool IsDeafened = false;
        private bool DirectionalAudio = false;
        private string Username = "";
        private string StatusMessage = "Connecting...";
        private DateTime RecordDetection = DateTime.UtcNow;

        private List<ParticipantModel> Participants;
        private MixingSampleProvider Mixer;
        private SoftLimiter Normalizer;
        private OpusEncoder Encoder;
        private IWaveIn AudioRecorder;
        private IWavePlayer AudioPlayer;

        private SignallingClient SignalClient;
        private VoiceClient VCClient;

        public static WaveFormat GetRecordFormat { get => new WaveFormat(SampleRate, 16, Channels); }
        public static WaveFormat GetAudioFormat { get => WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels * 2); }

        public async Task Run(CancellationToken ct, string serverName, bool directionalAudioEnabled)
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
                        MessagingCenter.Send(message, "Error");
                    });
                    return;
                }

                DirectionalAudio = directionalAudioEnabled;

                Participants = new List<ParticipantModel>();

                SignalClient = new SignallingClient();
                VCClient = new VoiceClient();

                IAudioManager audioManager = DependencyService.Get<IAudioManager>();
                Mixer = new MixingSampleProvider(GetAudioFormat) { ReadFully = true };
                Normalizer = new SoftLimiter(Mixer);
                Normalizer.Boost.CurrentValue = 10;

                Encoder = new OpusEncoder(SampleRate, Channels, Concentus.Enums.OpusApplication.OPUS_APPLICATION_VOIP);
                Encoder.Complexity = 10;
                Encoder.Bitrate = 64000;

                AudioRecorder = audioManager.CreateRecorder(GetRecordFormat);
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

                MessagingCenter.Subscribe<MuteUnmuteMessage>(this, "MuteUnmute", message =>
                {
                    IsMuted = !IsMuted;
                });

                MessagingCenter.Subscribe<DeafenUndeafenMessage>(this, "DeafenUndeafen", message =>
                {
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
                    Mixer.RemoveAllMixerInputs();
                    Encoder = null;
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
            Mixer.AddMixerInput(participant.MonoToStereo);
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
                    Mixer.RemoveMixerInput(participant.MonoToStereo);
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

        private Task VC_OnAudioReceived(byte[] Audio, string Key, float Volume, int BytesRecorded, float RotationSource)
        {
            _ = Task.Factory.StartNew(() =>
            {
                var participant = Participants.FirstOrDefault(x => x.LoginKey == Key);
                if (participant != null)
                {
                    try
                    {
                        short[] decoded = new short[1920];
                        participant.Decoder.Decode(Audio, 0, Audio.Length, decoded, 0, decoded.Length, false);
                        byte[] decodedBytes = ShortsToBytes(decoded, 0, decoded.Length);
                        participant.FloatProvider.Volume = Volume;
                        if (DirectionalAudio)
                        {
                            participant.MonoToStereo.LeftVolume = (float)(0.5 + Math.Sin(RotationSource) * 0.5);
                            participant.MonoToStereo.RightVolume = (float)(0.5 - Math.Sin(RotationSource) * 0.5);
                        }
                        participant.WaveProvider.AddSamples(decodedBytes, 0, BytesRecorded);
                    }
                    catch { }
                }
            });

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
            if (IsDeafened || IsMuted)
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
                short[] pcm = BytesToShorts(e.Buffer, 0, e.BytesRecorded);
                byte[] encoded = new byte[1000];
                var encodedBytes = Encoder.Encode(pcm, 0, 1920, encoded, 0, encoded.Length);
                byte[] trimmedBytes = encoded.SkipLast(1000 - encodedBytes).ToArray();
                if (encodedBytes > 0)
                {
                    var voicePacket = new VoicePacket()
                    {
                        PacketAudio = trimmedBytes,
                        PacketDataIdentifier = PacketIdentifier.Audio,
                        PacketVersion = Network.Network.Version,
                        PacketBytesRecorded = e.BytesRecorded
                    };
                    VCClient.Send(voicePacket);
                }
            }
        }

        private static short[] BytesToShorts(byte[] input, int offset, int length)
        {
            short[] processedValues = new short[length / 2];
            for (int c = 0; c < processedValues.Length; c++)
            {
                processedValues[c] = (short)(((int)input[(c * 2) + offset]) << 0);
                processedValues[c] += (short)(((int)input[(c * 2) + 1 + offset]) << 8);
            }

            return processedValues;
        }

        private static byte[] ShortsToBytes(short[] input, int offset, int length)
        {
            byte[] processedValues = new byte[length * 2];
            for (int c = 0; c < length; c++)
            {
                processedValues[c * 2] = (byte)(input[c + offset] & 0xFF);
                processedValues[c * 2 + 1] = (byte)((input[c + offset] >> 8) & 0xFF);
            }

            return processedValues;
        }
    }
}
