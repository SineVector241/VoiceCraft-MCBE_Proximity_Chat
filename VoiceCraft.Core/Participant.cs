using System;

namespace VoiceCraft.Core
{
    public abstract class Participant
    {
        public string Name { get; set; }
        public bool Deafened { get; set; }
        public bool Muted { get; set; }
        public long LastSpoke { get; set; }

        public Participant(string name)
        {
            Name = name;
        }
    }
}
