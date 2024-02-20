using OpusSharp;

namespace VoiceCraft.Maui.Views
{
    public partial class Servers : ContentPage
    {
        int count = 0;

        public Servers()
        {
            InitializeComponent();
            var enc = new OpusDecoder(48000, 1);
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;
            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
