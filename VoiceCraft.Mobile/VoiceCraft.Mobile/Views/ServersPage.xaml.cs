using VoiceCraft.Mobile.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace VoiceCraft.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ServersPage : ContentPage
    {
        public ServersPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if(Preferences.Get("VoipServiceRunning", false))
            {
                Device.BeginInvokeOnMainThread(() => { Shell.Current.GoToAsync(nameof(VoicePage)); });
            }
        }
    }
}