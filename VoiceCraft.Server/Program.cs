namespace VoiceCraft.Server
{
    public class Program
    {
        static void Main(string[] _) {
            Console.Title = $"VoiceCraft - {MainEntry.Version}: Starting...";
            new MainEntry().Start().GetAwaiter().GetResult();
        }
    }
}