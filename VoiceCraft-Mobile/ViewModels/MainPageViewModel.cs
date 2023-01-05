using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft_Mobile.Models;
using VoiceCraft_Mobile.Network;
using VoiceCraft_Mobile.Views;
using Android.Media;
using VoiceCraft_Mobile.Utils;
using Java.Util.Concurrent;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace VoiceCraft_Mobile.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private float _microphoneInput = 0f;

        [ObservableProperty]
        private bool _isMuted = false;

        private byte[] Buffer = new byte[G722ChatCodec.CodecInstance.RecordFormat.SampleRate / 10];
        private Task task = null;
        private bool cancel = false;
#if ANDROID
        private AudioRecord audioRecord = new AudioRecord(AudioSource.Mic, G722ChatCodec.CodecInstance.RecordFormat.SampleRate, ChannelIn.Mono, Encoding.Pcm16bit, G722ChatCodec.CodecInstance.RecordFormat.SampleRate / 10 );
#endif
#if IOS || MACCATALYST
#endif
        public MainPageViewModel()
        {
            cancel = false;
            var executor = Executors.NewSingleThreadExecutor();
            Clients = new ObservableCollection<Client>();
            audioRecord.StartRecording();
            task = Task.Run(() =>
            {
                while (true)
                {
                    audioRecord.Read(Buffer, 0, 1600);
                    float max = 0;
                    for (int index = 0; index < Buffer.Length; index += 2)
                    {
                        short sample = (short)((Buffer[index + 1] << 8) |
                                                Buffer[index + 0]);
                        var sample32 = sample / 32768f;
                        if (sample32 < 0) sample32 = -sample32;
                        if (sample32 > max) max = sample32;
                    }
                    MicrophoneInput = max * 100;
                    var encoded = G722ChatCodec.CodecInstance.Encode(Buffer, 0, Buffer.Length);

                    if (UdpNetworkHandler.Instance._IsLoggedIn && !IsMuted && MicrophoneInput > 2)
                        UdpNetwork.Instance.SendPacket(new Packet() { VCPacketDataIdentifier = PacketIdentifier.AudioStream, VCSessionKey = UdpNetworkHandler.Instance._Key, VCAudioBuffer = encoded });

                    if (cancel) break;
                }
            });

            UdpNetworkHandler.Instance.Ready();
            UdpNetworkHandler.Instance.Logout += OnLogout;
            UdpNetworkHandler.Instance.ClientLogin += OnClientLogin;
            UdpNetworkHandler.Instance.ClientLogout += OnClientLogout;
        }

        //Commands
        [ICommand]
        void MuteUmute()
        {
            
        }

        [ICommand]
        async void Disconnect()
        {
            UdpNetworkHandler.Instance.Disconnect();
            audioRecord.Stop();
            audioRecord.Release();
            cancel = true;
            System.Threading.Thread.Sleep(1000);
            if (task != null) task.Dispose();
            await Shell.Current.GoToAsync($"///./{nameof(LoginPage)}");
        }

        //Methods
        private void OnClientLogout(object? sender, ClientLogoutEventArgs e)
        {
            var client = Clients.FirstOrDefault(x => x.SessionKey == e.SessionKey);
            if (client != null) Clients.Remove(client);
        }

        private void OnClientLogin(object? sender, ClientLoginEventArgs e)
        {
            Clients.Add(new Client() { Username = e.Username, SessionKey = e.SessionKey });
        }

        private async void OnLogout(object? sender, System.EventArgs e)
        {
            audioRecord.Stop();
            audioRecord.Release();
            cancel = true;
            System.Threading.Thread.Sleep(1000);
            if (task != null) task.Dispose();
            await Shell.Current.GoToAsync($"///./{nameof(LoginPage)}");
        }
    }
}
