namespace VoiceCraft.Server
{
    public class Logger
    {
        public static void LogToConsole(LogType logType, string message, string tag)
        {
            switch(logType)
            {
                case LogType.Info:
                    Console.ResetColor();
                    Console.WriteLine($"[{DateTime.Now}] [{tag}]: {message}");
                    break;

                case LogType.Error: 
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now}] [Error] [{tag}]: {message}");
                    Console.ResetColor();
                    break;

                case LogType.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[{DateTime.Now}] [Warning] [{tag}]: {message}");
                    Console.ResetColor();
                    break;

                case LogType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{DateTime.Now}] [{tag}]: {message}");
                    Console.ResetColor();
                    break;
            }
        }
    }

    public enum LogType
    {
        Info,
        Warn,
        Error,
        Success
    }
}
