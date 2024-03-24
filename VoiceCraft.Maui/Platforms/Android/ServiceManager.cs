using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using CommunityToolkit.Mvvm.Messaging;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Models;
using VoiceCraft.Maui.Services;
using VoiceCraft.Client;

namespace VoiceCraft.Maui
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
    public class ServiceManager : Service, IServiceManager
    {
        private CancellationTokenSource? Cts;
        private VoipService? VoipService;
        private Task? easterEggLoop;

        private string NOTIFICATION_CHANNEL_ID = "1000";
        private int NOTIFICATION_ID = 1;
        private string NOTIFICATION_CHANNEL_NAME = "Voice";

        string[] splashEasterEggs = {
            "1+1 = window.",
            "creeper, aww man.", 
            "ANDROID FTW.",
            "PC master race.", "No way dude, That's insane.",
            "What came first, the chicken or the egg?",
            "Android version only has easter eggs.",
            "The wheel's on a bus go... oh nevermind.",
            "Press F for help.",
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            $"{Math.PI}",
            "Baby shark do doo do doo do do"
        };

        public void StartService()
        {
            try
            {
                Preferences.Set("VoipServiceRunning", true);
                var serviceIntent = new Intent(Android.App.Application.Context, typeof(ServiceManager));
                Android.App.Application.Context.StartForegroundService(serviceIntent);
            }
            catch
            {
                Preferences.Set("VoipServiceRunning", false);
            }
        }

        public void Stop() 
        {
            try
            {
                Preferences.Set("VoipServiceRunning", false);
                var serviceIntent = new Intent(Android.App.Application.Context, typeof(ServiceManager));
                Android.App.Application.Context.StopService(serviceIntent);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private NotificationCompat.Builder CreateNotification(NotificationManager notificationManager)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME, NotificationImportance.Low);
                notificationManager?.CreateNotificationChannel(channel);
            }

            var nBuilder = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("Starting...")
                .SetContentText("Starting VoiceCraft...")
                .SetSmallIcon(Resource.Drawable.microphone)
                .SetOngoing(true);

            return nBuilder;
        }

        private void StartVoiceCraftService()
        {
            var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
            if (notificationManager == null) throw new Exception("Notification manager was null");

            StartForeground(NOTIFICATION_ID, CreateNotification(notificationManager).Build());
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                try
                {
                    StartVoiceCraftService();
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

                    easterEggLoop = Task.Run(async () => {
                        while(true)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(2));
                            var random = new Random();

                            if (!Cts.IsCancellationRequested && VoipService != null)
                            {
                                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                                if (notificationManager != null) notificationManager.Notify(NOTIFICATION_ID, CreateNotification(notificationManager).SetContentTitle(VoipService.StatusMessage).SetContentText(splashEasterEggs[random.Next(0, splashEasterEggs.Length - 1)]).Build());
                            }
                            else
                            {
                                break;
                            }
                        }
                    }, Cts.Token);
                    VoipService.Start(Cts.Token).Wait();
                }
                catch (System.OperationCanceledException)
                {
                }
                finally
                {
                    if (VoipService != null)
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
                    }

                    WeakReferenceMessenger.Default.UnregisterAll(this);
                    Stop();
                    Cts.Dispose();
                }
            }, Cts.Token);
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            try
            {
                Preferences.Set("VoipServiceRunning", false); //Set to false anyways.
                if (!Cts?.IsCancellationRequested ?? false)
                {
                    Cts?.Cancel();
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
                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                if (notificationManager != null) notificationManager.Notify(NOTIFICATION_ID, CreateNotification(notificationManager).SetContentTitle(status).SetContentText("Idling...").Build());

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
                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                if (notificationManager != null) notificationManager.Notify(NOTIFICATION_ID, CreateNotification(notificationManager).SetContentTitle(VoipService?.StatusMessage ?? "Disconnected").SetContentText($"{participant.Name} has connected!").Build());

                WeakReferenceMessenger.Default.Send(new ParticipantAddedMSG(participant));
            });
        }

        private void ParticipantRemoved(VoiceCraftParticipant participant)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
                if (notificationManager != null) notificationManager.Notify(NOTIFICATION_ID, CreateNotification(notificationManager).SetContentTitle(VoipService?.StatusMessage ?? "Disconnected").SetContentText($"{participant.Name} has disconnected!").Build());

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
            Cts?.Cancel();
        }
        private void OnDeny(Core.Packets.Signalling.Deny data, System.Net.Sockets.Socket socket)
        {
            if (VoipService?.Network.Signalling.IsConnected ?? false)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    WeakReferenceMessenger.Default.Send(new DenyMSG(data.Reason));
                });
            }
        }
    }
}
