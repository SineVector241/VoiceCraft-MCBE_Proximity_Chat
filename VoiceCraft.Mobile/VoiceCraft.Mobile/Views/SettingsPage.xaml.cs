using System;
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
    }
}