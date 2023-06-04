using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VoiceCraft
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            testImg.Source = ImageSource.FromResource("VoiceCraft.Resources.Images.bgdark.png");
        }
    }
}
