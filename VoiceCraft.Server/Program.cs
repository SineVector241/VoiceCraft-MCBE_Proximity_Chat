namespace VoiceCraft.Server
{
    public class Program
    {
        static void Main(string[] _) {
            new MainEntry().Start().GetAwaiter().GetResult();
        }
    }
}