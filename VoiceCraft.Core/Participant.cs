using System;

namespace VoiceCraft.Core
{
    public abstract class Participant
    {
        public string Name { get; set; }
        public int LastActive { get; set; }
        public ushort PublicId { get; set; }

        public Participant(string name, ushort publicId)
        {
            Name = name;
            LastActive = Environment.TickCount;
            PublicId = publicId;
        }
    }
}
