using System;

namespace VoiceCraft.Core
{
    public abstract class Participant
    {
        public string Name { get; set; }

        public Participant(string name)
        {
            Name = name;
        }
    }
}
