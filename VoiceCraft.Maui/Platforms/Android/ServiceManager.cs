using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Core.Client;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Services;
using VoiceCraft.Models;

namespace VoiceCraft.Maui
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
    public class ServiceManager : Service
    {
        CancellationTokenSource Cts;
        private VoipService VoipService;

        private string NOTIFICATION_CHANNEL_ID = "1000";
        private int NOTIFICATION_ID = 1;
        private string NOTIFICATION_CHANNEL_NAME = "notification";

        public void StartService()
        {
            Intent intent = new Intent(Android.App.Application.Context, typeof(ServiceManager));
            StartForegroundService(intent);
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

                    WeakReferenceMessenger.Default.Register(this, (object recipient, RequestData message) =>
                    {
                        WeakReferenceMessenger.Default.Send(new ResponseData()
                        {
                            Channels = VoipService.Network.Channels.Select(x => new ChannelModel(x)).ToList(),
                            Participants = VoipService.Network.Participants.Select(x => new ParticipantModel(x.Value)).ToList(),
                            StatusMessage = VoipService.StatusMessage,
                            IsDeafened = VoipService.Network.IsDeafened,
                            IsMuted = VoipService.Network.IsMuted
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

                    WeakReferenceMessenger.Default.Register(this, (object recipient, DisconnectedMSG message) =>
                    {
                        Cts.Cancel();
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, JoinChannelMSG message) =>
                    {
                        VoipService.Network.JoinChannel(message.Channel, message.Password);
                    });

                    WeakReferenceMessenger.Default.Register(this, (object recipient, LeaveChannelMSG message) =>
                    {
                        VoipService.Network.LeaveChannel(message.Channel);
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
            WeakReferenceMessenger.Default.Send(new StatusMessageUpdatedMSG() { Status = status });
        }

        private void SpeakingStatusChanged(bool status)
        {
            WeakReferenceMessenger.Default.Send(new SpeakingStatusChangedMSG() { Status = status });
        }

        private void MutedStatusChanged(bool status)
        {
            WeakReferenceMessenger.Default.Send(new MutedStatusChangedMSG() { Status = status });
        }

        private void DeafenedStatusChanged(bool status)
        {
            WeakReferenceMessenger.Default.Send(new DeafenedStatusChangedMSG() { Status = status });
        }

        private void ParticipantAdded(VoiceCraftParticipant participant)
        {
            WeakReferenceMessenger.Default.Send(new ParticipantAddedMSG(participant));
        }

        private void ParticipantRemoved(VoiceCraftParticipant participant)
        {
            WeakReferenceMessenger.Default.Send(new ParticipantRemovedMSG(participant));
        }
        private void ParticipantSpeakingStatusChanged(VoiceCraftParticipant participant, bool status)
        {
            WeakReferenceMessenger.Default.Send(new ParticipantSpeakingStatusChangedMSG(participant) { Status = status });
        }
        private void ParticipantChanged(VoiceCraftParticipant participant)
        {
            WeakReferenceMessenger.Default.Send(new ParticipantChangedMSG(participant));
        }
        private void ChannelCreated(VoiceCraftChannel channel)
        {
            WeakReferenceMessenger.Default.Send(new ChannelCreatedMSG(channel));
        }
        private void ChannelEntered(VoiceCraftChannel channel)
        {
            WeakReferenceMessenger.Default.Send(new ChannelEnteredMSG(channel));
        }
        private void ChannelLeave(VoiceCraftChannel channel)
        {
            WeakReferenceMessenger.Default.Send(new ChannelLeftMSG(channel));
        }
        private void OnServiceDisconnected(string? reason)
        {
            WeakReferenceMessenger.Default.Send(new DisconnectedMSG() { Reason = reason ?? string.Empty });
            Cts.Cancel();
        }
        private void SignallingDeny(Core.Packets.Signalling.Deny packet)
        {
            if (!packet.Disconnect)
            {
                WeakReferenceMessenger.Default.Send(new DenyMSG() { Reason = packet.Reason });
            }
        }
    }
}
