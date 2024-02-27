namespace VoiceCraft.Core
{
    public abstract class Channel
    {
        public string Name { get; }

        public Channel(string name)
        {
            Name = name;
        }
    }
}
