using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core.Client;
using VoiceCraft.Mobile.Services;
using VoiceCraft.Mobile.Models;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.Droid.Services
{
    [Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMicrophone)]
    public class VoipForegroundService : Service
    {
        CancellationTokenSource cts;
        public const int ServiceRunningNotificationId = 28234;
        private VoipService voipService;
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            cts = new CancellationTokenSource();
            Notification notification = new NotificationHelper().GetServiceStartedNotification();
            StartForeground(ServiceRunningNotificationId, notification);

            Task.Run(() =>
            {
                try
                {
                    voipService = new VoipService();
                    voipService.OnStatusUpdated += StatusUpdated;
                    voipService.OnSpeakingStatusChanged += SpeakingStatusChanged;
                    voipService.OnMutedStatusChanged += MutedStatusChanged;
                    voipService.OnDeafenedStatusChanged += DeafenedStatusChanged;
                    voipService.OnParticipantAdded += ParticipantAdded;
                    voipService.OnParticipantRemoved += ParticipantRemoved;
                    voipService.OnParticipantSpeakingStatusChanged += ParticipantSpeakingStatusChanged;
                    voipService.OnParticipantChanged += ParticipantChanged;
                    voipService.OnChannelCreated += ChannelCreated;
                    voipService.OnChannelEntered += ChannelEntered;
                    voipService.OnChannelLeave += ChannelLeave;
                    voipService.OnServiceDisconnected += OnServiceDisconnected;
                    voipService.Network.Signalling.OnDenyPacketReceived += SignallingDeny;

                    MessagingCenter.Subscribe<RequestData>(this, "RequestData", message =>
                    {
                        MessagingCenter.Send(new ResponseData()
                        {
                            Channels = voipService.Network.Channels.Select(x => new ChannelDisplayModel(x)).ToList(),
                            Participants = voipService.Network.Participants.Select(x => new ParticipantDisplayModel(x.Value)).ToList(),
                            StatusMessage = voipService.StatusMessage,
                            IsDeafened = voipService.Network.IsDeafened,
                            IsMuted = voipService.Network.IsMuted
                        }, "ResponseData");
                    });

                    MessagingCenter.Subscribe<MuteUnmuteMSG>(this, "MuteUnmute", message =>
                    {
                        voipService.Network.SetMute();
                    });

                    MessagingCenter.Subscribe<DeafenUndeafenMSG>(this, "DeafenUndeafen", message =>
                    {
                        voipService.Network.SetDeafen();
                    });

                    MessagingCenter.Subscribe<DisconnectMSG>(this, "Disconnect", message =>
                    {
                        cts.Cancel();
                    });

                    voipService.Start(cts.Token).Wait();
                }
                catch (System.OperationCanceledException)
                {
                }
                finally
                {
                    voipService.OnStatusUpdated -= StatusUpdated;
                    voipService.OnSpeakingStatusChanged -= SpeakingStatusChanged;
                    voipService.OnMutedStatusChanged -= MutedStatusChanged;
                    voipService.OnDeafenedStatusChanged -= DeafenedStatusChanged;
                    voipService.OnParticipantAdded -= ParticipantAdded;
                    voipService.OnParticipantRemoved -= ParticipantRemoved;
                    voipService.OnParticipantSpeakingStatusChanged -= ParticipantSpeakingStatusChanged;
                    voipService.OnParticipantChanged -= ParticipantChanged;
                    voipService.OnChannelCreated -= ChannelCreated;
                    voipService.OnChannelEntered -= ChannelEntered;
                    voipService.OnChannelLeave -= ChannelLeave;
                    voipService.OnServiceDisconnected -= OnServiceDisconnected;
                    voipService.Network.Signalling.OnDenyPacketReceived -= SignallingDeny;

                    MessagingCenter.Unsubscribe<RequestData>(this, "RequestData");
                    MessagingCenter.Unsubscribe<MuteUnmuteMSG>(this, "MuteUnmute");
                    MessagingCenter.Unsubscribe<DeafenUndeafenMSG>(this, "DeafenUndeafen");
                    MessagingCenter.Unsubscribe<DisconnectMSG>(this, "Disconnect");
                    MessagingCenter.Send(new StopServiceMSG(), "StopService");
                    cts.Dispose();
                }
            }, cts.Token);
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            try
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                    Preferences.Set("VoipServiceRunning", false);
                }
            }
            catch(System.OperationCanceledException)
            {
                //Do nothing
            }
            base.OnDestroy();
        }

        private void StatusUpdated(string status)
        {
            MessagingCenter.Send(new StatusMessageUpdatedMSG() { Status = status }, "StatusMessageUpdated");
        }

        private void SpeakingStatusChanged(bool status)
        {
            MessagingCenter.Send(new SpeakingStatusChangedMSG() { Status = status }, "SpeakingStatusChanged");
        }

        private void MutedStatusChanged(bool status)
        {
            MessagingCenter.Send(new MutedStatusChangedMSG() { Status = status }, "MutedStatusChanged");
        }

        private void DeafenedStatusChanged(bool status)
        {
            MessagingCenter.Send(new DeafenedStatusChangedMSG() { Status = status }, "DeafenedStatusChanged");
        }

        private void ParticipantAdded(VoiceCraftParticipant participant)
        {
            MessagingCenter.Send(new ParticipantAddedMSG(participant), "ParticipantAdded");
        }

        private void ParticipantRemoved(VoiceCraftParticipant participant)
        {
            MessagingCenter.Send(new ParticipantRemovedMSG(participant), "ParticipantRemoved");
        }
        private void ParticipantSpeakingStatusChanged(VoiceCraftParticipant participant, bool status)
        {
            MessagingCenter.Send(new ParticipantSpeakingStatusChangedMSG(participant) { Status = status }, "ParticipantSpeakingStatusChanged");
        }
        private void ParticipantChanged(VoiceCraftParticipant participant)
        {
            MessagingCenter.Send(new ParticipantChangedMSG(participant), "ParticipantChanged");
        }
        private void ChannelCreated(VoiceCraftChannel channel)
        {
            MessagingCenter.Send(new ChannelCreatedMSG(channel), "ChannelCreated");
        }
        private void ChannelEntered(VoiceCraftChannel channel)
        {
            MessagingCenter.Send(new ChannelEnteredMSG(channel), "ChannelEntered");
        }
        private void ChannelLeave(VoiceCraftChannel channel)
        {
            MessagingCenter.Send(new ChannelLeftMSG(channel), "ChannelLeft");
        }
        private void OnServiceDisconnected(string reason)
        {
            MessagingCenter.Send(new DisconnectedMSG() { Reason = reason }, "Disconnected");
            cts.Cancel();
        }
        private void SignallingDeny(Core.Packets.Signalling.Deny packet)
        {
            if(!packet.Disconnect)
            {
                MessagingCenter.Send(new DenyMSG() { Reason = packet.Reason }, "Deny");
            }
        }
    }
}