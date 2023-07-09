using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace VoiceCraft.Windows.Network.Builders
{
    //https://gist.github.com/jocopa3/5f718f4198f1ea91a37e3a9da468675c
    public enum EventType
    {
        [Description("PlayerTravelled")]
        PlayerTravelled

    }

    public class EventBuilder
    {
        private EventStructure eventStructure = new EventStructure();

        public EventBuilder SetMessagePurpose(string messagePurpose)
        {
            eventStructure.header.messagePurpose = messagePurpose;
            return this;
        }

        public EventBuilder SetEventType(EventType eventType)
        {
            eventStructure.body.eventName = eventType.ToDescriptionString();
            return this;
        }

        public string Build()
        {
            if (string.IsNullOrWhiteSpace(eventStructure.body.eventName))
                throw new Exception("Error. EventType must be set!");

            string convert = JsonConvert.SerializeObject(eventStructure);
            return convert;
        }

        private class EventStructure
        {
            public EventHeaders header { get; set; } = new EventHeaders();
            public EventBody body { get; set; } = new EventBody();
        }

        private class EventHeaders
        {
            public string requestId { get; set; } = Guid.NewGuid().ToString();
            public string messagePurpose { get; set; } = "subscribe";
            public int version { get; set; } = 1;
        }

        private class EventBody
        {
            public string eventName { get; set; } = "";
        }
    }

    public static class EnumExtension
    {
        public static string ToDescriptionString(this EventType val)
        {
            DescriptionAttribute[] attributes = (DescriptionAttribute[])val
               .GetType()
               .GetField(val.ToString())
               .GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : string.Empty;
        }
    }
}