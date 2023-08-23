using System;
using Xamarin.Forms;

namespace VoiceCraft.Mobile.Animations
{
    public class HideAction : TriggerAction<VisualElement>
    {
        protected override async void Invoke(VisualElement sender)
        {
            await sender.TranslateTo(0, -5, 100);
            await sender.TranslateTo(0, 300, 300);
        }
    }
}
