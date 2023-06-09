using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using VoiceCraft.Mobile.Models;
using VoiceCraft.Mobile.Network.Packets;
using VoiceCraft.Mobile.Storage;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.ViewModels
{
    public partial class ServerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        string externalServerInformation = "Pinging...";

        [ObservableProperty]
        ServerModel server;

        public ServerPageViewModel()
        {
            Server = Database.GetPassableObject<ServerModel>();
        }

        [RelayCommand]
        public void Test()
        {
            var packet = new SignallingPacket() { PacketCodec = Network.Codecs.AudioCodecs.G722, PacketIdentifier = SignallingPacketIdentifiers.Accept16, PacketKey = 56, PacketVoicePort = 9051, PacketMetadata = "Test", PacketVersion = "testaaa" }.GetPacketDataStream();
            var decoded = new SignallingPacket(packet);
            Console.WriteLine(decoded.PacketIdentifier);
            Console.WriteLine(decoded.PacketCodec);
            Console.WriteLine(decoded.PacketKey);
            Console.WriteLine(decoded.PacketVoicePort);
            Console.WriteLine(decoded.PacketVersion);
            Console.WriteLine(decoded.PacketMetadata);
        }
    }
}
