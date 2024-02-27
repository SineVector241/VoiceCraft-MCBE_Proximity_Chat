using System;

namespace VoiceCraft.Core
{
    public abstract class Participant
    {
        public string Name { get; set; }
        public int LastActive { get; set; }

        public Participant(string name)
        {
            Name = name;
            LastActive = Environment.TickCount;
        }
    }
}
