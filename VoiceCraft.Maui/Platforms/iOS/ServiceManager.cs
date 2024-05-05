using AVFoundation;
using Foundation;

using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Models;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Client;

namespace VoiceCraft.Maui
{
    public class ServiceManager : IServiceManager
    {
        private CancellationTokenSource Cts;
        private VoipService VoipService;

        public ServiceManager()
        {
            Cts = new CancellationTokenSource();
            VoipService = new VoipService(Navigator.GetNavigationData<ServerModel>());
        }

        public void ConfigureAudioSessionOnStart()
        {
            var audioSession = AVAudioSession.SharedInstance();
            NSError error;

            audioSession.SetCategory(new NSString(AVAudioSessionCategory.PlayAndRecord.ToString()), out error);
            if (error != null)
                Console.WriteLine($"Error setting audio session category: {error}");

            audioSession.SetActive(true, out error);
            if (error != null)
                Console.WriteLine($"Error activating audio session: {error}");
        }

        public void ConfigureAudioSessionOnEnd()
        {
            var audioSession = AVAudioSession.SharedInstance();
            NSError error;

            // Deactivate the audio session
            audioSession.SetActive(false, out error);
            if (error != null)
            {
                Console.WriteLine($"Error deactivating audio session: {error.LocalizedDescription}");
            }
        }


        public void StartService()
        {
            Task.Run(() =>
            {
                try
                {
                    Preferences.Set("VoipServiceRunning", true);
                    ConfigureAudioSessionOnStart();
                    VoipService.OnStatusUpdated += StatusUpdated;
                    VoipService.OnSpeakingStatusChanged += SpeakingStatusChanged;
                    VoipService.OnMutedStatusChanged += MutedStatusChanged;
                    VoipService.OnDeafenedStatusChanged += DeafenedStatusChanged;
                    VoipService.OnParticipantAdded += ParticipantAdded;
                    VoipService.OnParticipantRemoved += ParticipantRemoved;
                    VoipService.OnParticipantSpeakingStatusChanged += ParticipantSpeakingStatusChanged;
                    VoipService.OnParticipantChanged += ParticipantChanged;
                    VoipService.OnChannelCreated += ChannelCreated;
                    VoipService.OnChannelEntered += ChannelEntered;
                    VoipService.OnChannelLeave += ChannelLeave;
                    VoipService.OnServiceDisconnected += OnServiceDisconnected;
                    VoipService.Network.Signalling.OnDeny += OnDeny;

                    WeakReferenceMessenger.Default.Register(this, (object recipient, RequestDataMSG message) =>
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            WeakReferenceMessenger.Default.Send(new ResponseDataMSG(new ResponseData(
                                VoipService.Network.Participants.Select(x => new ParticipantModel(x.Value)).ToList(),
                                VoipService.Network.Channels.Select(x => new ChannelModel(x)).ToList(),
                                false,
                                VoipService.Network.IsMuted,
                                VoipService.Network.IsDeafened,
                                VoipService.StatusMessage)));
                        });
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, MuteUnmuteMSG message) =>
                    {
                        _ = VoipService.Network.SetMute();
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, DeafenUndeafenMSG message) =>
                    {
                        _ = VoipService.Network.SetDeafen();
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, DisconnectMSG message) =>
                    {
                        Cts.Cancel();
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, JoinChannelMSG message) =>
                    {
                        _ = VoipService.Network.JoinChannel(message.Value.Channel, message.Value.Password);
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, LeaveChannelMSG message) =>
                    {
                        _ = VoipService.Network.LeaveChannel(message.Value.Channel);
                    });

                    VoipService.Start(Cts.Token).Wait();
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    VoipService.OnStatusUpdated -= StatusUpdated;
                    VoipService.OnSpeakingStatusChanged -= SpeakingStatusChanged;
                    VoipService.OnMutedStatusChanged -= MutedStatusChanged;
                    VoipService.OnDeafenedStatusChanged -= DeafenedStatusChanged;
                    VoipService.OnParticipantAdded -= ParticipantAdded;
                    VoipService.OnParticipantRemoved -= ParticipantRemoved;
                    VoipService.OnParticipantSpeakingStatusChanged -= ParticipantSpeakingStatusChanged;
                    VoipService.OnParticipantChanged -= ParticipantChanged;
                    VoipService.OnChannelCreated -= ChannelCreated;
                    VoipService.OnChannelEntered -= ChannelEntered;
                    VoipService.OnChannelLeave -= ChannelLeave;
                    VoipService.OnServiceDisconnected -= OnServiceDisconnected;
                    VoipService.Network.Signalling.OnDeny -= OnDeny;

                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    ConfigureAudioSessionOnEnd();
                    Cts.Dispose();
                    Preferences.Set("VoipServiceRunning", false);
                }
            }, Cts.Token);
        }

        private void StatusUpdated(string status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new StatusMessageUpdatedMSG(status));
            });
        }

        private void SpeakingStatusChanged(bool status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new SpeakingStatusChangedMSG(status));
            });
        }

        private void MutedStatusChanged(bool status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new MutedStatusChangedMSG(status));
            });
        }

        private void DeafenedStatusChanged(bool status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new DeafenedStatusChangedMSG(status));
            });
        }

        private void ParticipantAdded(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ParticipantAddedMSG(participant));
            });
        }

        private void ParticipantRemoved(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ParticipantRemovedMSG(participant));
            });
        }
        private void ParticipantSpeakingStatusChanged(VoiceCraftParticipant participant, bool status)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ParticipantSpeakingStatusChangedMSG(new ParticipantSpeakingStatusChanged(participant, status)));
            });
        }
        private void ParticipantChanged(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ParticipantChangedMSG(participant));
            });
        }
        private void ChannelCreated(VoiceCraftChannel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ChannelCreatedMSG(channel));
            });
        }
        private void ChannelEntered(VoiceCraftChannel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ChannelEnteredMSG(channel));
            });
        }
        private void ChannelLeave(VoiceCraftChannel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new ChannelLeftMSG(channel));
            });
        }
        private void OnServiceDisconnected(string? reason)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                WeakReferenceMessenger.Default.Send(new DisconnectedMSG(reason ?? string.Empty));
            });
            Cts.Cancel();
        }
        private void OnDeny(Core.Packets.Signalling.Deny data, System.Net.Sockets.Socket socket)
        {
            if (VoipService.Network.Signalling.IsConnected)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new DenyMSG(data.Reason));
                });
            }
        }
    }
}
