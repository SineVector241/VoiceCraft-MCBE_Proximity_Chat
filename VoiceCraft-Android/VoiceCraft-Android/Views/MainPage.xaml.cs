using VoiceCraft_Android.ViewModels;
using Xamarin.Forms;

namespace VoiceCraft_Android
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            var viewModel = (MainPageViewModel)BindingContext;
            if (viewModel.AppearingCommand.CanExecute(null))
                viewModel.AppearingCommand.Execute(null);
        }
    }
}
