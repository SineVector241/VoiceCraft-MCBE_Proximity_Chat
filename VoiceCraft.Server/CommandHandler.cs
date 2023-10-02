namespace VoiceCraft.Server
{
    public class CommandHandler
    {
        private static Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>();

        public static void RegisterCommand(string CommandName, Action<string[]> Command)
        {
            if (!Commands.ContainsKey(CommandName))
            {
                Commands.Add(CommandName.ToLower(), Command);
            }
        }

        public static void ParseCommand(string Command)
        {
            string[] parts = Command.Split(' ');
            string command = parts[0];
            string[] arguments = parts.Length > 1 ? new ArraySegment<string>(parts, 1, parts.Length - 1).ToArray() : new string[0];

            if (Commands.ContainsKey(command))
            {
                // Execute the command with the arguments
                Commands[command](arguments);
            }
            else
            {
                throw new Exception("Invalid command. Type 'help' for a list of available commands.");
            }
        }
    }
}
