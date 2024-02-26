using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Core.Client;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Services;

namespace VoiceCraft.Maui
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
    public class ServiceManager : Service, IServiceManager
    {
        private CancellationTokenSource Cts;
        private VoipService VoipService;
        private Intent serviceIntent;

        private string NOTIFICATION_CHANNEL_ID = "1000";
        private int NOTIFICATION_ID = 1;
        private string NOTIFICATION_CHANNEL_NAME = "notification";

        public void StartService()
        {
            serviceIntent = new Intent(Android.App.Application.Context, typeof(ServiceManager));
            StartForegroundService(serviceIntent);
        }

        private void StartVoiceCraftService()
        {
            var notifcationManager = GetSystemService(Context.NotificationService) as NotificationManager;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME, NotificationImportance.Low);
                notifcationManager?.CreateNotificationChannel(channel);
            }

            var nBuilder = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("VoiceCraft")
                .SetContentText("Voice Ongoing...")
                .SetSmallIcon(Resource.Drawable.microphone)
                .SetOngoing(true);

            StartForeground(NOTIFICATION_ID, nBuilder.Build());
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Cts = new CancellationTokenSource();
            StartVoiceCraftService();
            Task.Run(() =>
            {
                try
                {
                    Preferences.Set("VoipServiceRunning", true);
                    VoipService = new VoipService(Navigator.GetNavigationData<ServerModel>());
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
                    VoipService.Network.Signalling.OnDenyPacketReceived += SignallingDeny;

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
                        VoipService.Network.SetMute();
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, DeafenUndeafenMSG message) =>
                    {
                        VoipService.Network.SetDeafen();
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, DisconnectMSG message) =>
                    {
                        Cts.Cancel();
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, JoinChannelMSG message) =>
                    {
                        VoipService.Network.JoinChannel(message.Value.Channel, message.Value.Password);
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, LeaveChannelMSG message) =>
                    {
                        VoipService.Network.LeaveChannel(message.Value.Channel);
                    });

                    VoipService.Start(Cts.Token).Wait();
                }
                catch (System.OperationCanceledException)
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
                    VoipService.Network.Signalling.OnDenyPacketReceived -= SignallingDeny;

                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    Preferences.Set("VoipServiceRunning", false);
                    StopService(serviceIntent);
                    Cts.Dispose();
                }
            }, Cts.Token);
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            try
            {
                if (!Cts.IsCancellationRequested)
                {
                    Cts.Cancel();
                    Preferences.Set("VoipServiceRunning", false);
                }
            }
            catch (System.OperationCanceledException)
            {
                //Do nothing
            }
            base.OnDestroy();
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
        private void SignallingDeny(Core.Packets.Signalling.Deny packet)
        {
            if (!packet.Disconnect)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new DenyMSG(packet.Reason));
                });
            }
        }
    }
}
