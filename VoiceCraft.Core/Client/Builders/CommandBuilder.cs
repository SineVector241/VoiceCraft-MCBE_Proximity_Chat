using Newtonsoft.Json;
using System;

namespace VoiceCraft.Core.Client.Builders
{
    public class CommandBuilder
    {
        private CommandStructure commandStructure = new CommandStructure();

        public CommandBuilder SetCommand(string command)
        {
            commandStructure.body.commandLine = command;
            return this;
        }

        public string Build()
        {
            if (string.IsNullOrWhiteSpace(commandStructure.body.commandLine))
                throw new Exception("Error. Command must be set!");

            string convert = JsonConvert.SerializeObject(commandStructure);
            return convert;
        }

        private class CommandStructure
        {
            public CommandHeaders header { get; set; } = new CommandHeaders();
            public CommandBody body { get; set; } = new CommandBody();
        }

        private class CommandHeaders
        {
            public string requestId { get; set; } = Guid.NewGuid().ToString();
            public string messagePurpose { get; set; } = "commandRequest";
            public int version { get; set; } = 1;
            public string messageType { get; set; } = "commandRequest";
        }

        private class CommandBody
        {
            public CommandOrigin origin = new CommandOrigin();
            public string commandLine { get; set; } = "";
        }

        private class CommandOrigin
        {
            public string type = "player";
        }
    }
}