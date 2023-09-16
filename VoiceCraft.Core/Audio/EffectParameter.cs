using System;

namespace VoiceCraft.Core.Audio
{
    class EffectParameter
    {
        public float Min { get; }
        public float Max { get; }
        public string Description { get; }
        private float currentValue;
        public event EventHandler? ValueChanged;
        public float CurrentValue
        {
            get { return currentValue; }
            set
            {
                if (value < Min || value > Max)
                    throw new ArgumentOutOfRangeException(nameof(CurrentValue));
                if (currentValue != value)
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                currentValue = value;
            }
        }

        public EffectParameter(float defaultValue, float minimum, float maximum, string description)
        {
            Min = minimum;
            Max = maximum;
            Description = description;
            CurrentValue = defaultValue;
        }
    }
}

//Credits: https://www.markheath.net/post/limit-audio-naudio