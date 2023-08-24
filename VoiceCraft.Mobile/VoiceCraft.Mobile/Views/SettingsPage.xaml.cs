using System;
using VoiceCraft.Mobile.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace VoiceCraft.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void GainValueChanged(object sender, ValueChangedEventArgs e)
        {
            GainLabel.Text = $"SoftLimiter Gain(DB): {Math.Round(e.NewValue, 2)}";
        }

        private void MicDetectionValueChanged(object sender, ValueChangedEventArgs e)
        {
            MicLabel.Text = $"Microphone Detection: {Math.Round(e.NewValue, 2)}";
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            var viewModel = (SettingsPageViewModel)BindingContext;
            if (viewModel.DisappearingCommand.CanExecute(null))
                viewModel.DisappearingCommand.Execute(null);
        }

        protected override void OnAppearing()
        {
            base.OnDisappearing();
            var viewModel = (SettingsPageViewModel)BindingContext;
            if (viewModel.AppearingCommand.CanExecute(null))
                viewModel.AppearingCommand.Execute(null);
        }
    }
}