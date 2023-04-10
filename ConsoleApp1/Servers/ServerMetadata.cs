using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Threading;

namespace VoiceCraft_Server
{
    public class ServerMetadata
    {
        public static List<Participant> voiceParticipants { get; set; } = new List<Participant>();
        public static Timer timer = null;

        //Events
        public delegate void ParicipantLogout(Participant participant);

        public static event ParicipantLogout OnParticipantLogout;
            
        public static void CheckParticipants(object state)
        {
            for(int i = 0; i < voiceParticipants.Count; i++)
            {
                if (DateTime.UtcNow.Subtract(voiceParticipants[i].LastPing).Seconds > 10)
                {
                    Logger.LogToConsole(LogType.Warn, $"Removed Client - Username: {voiceParticipants[i].Name} Key:{voiceParticipants[i].LoginId} Reason: TimeOut", nameof(ServerMetadata));
                    OnParticipantLogout?.Invoke(voiceParticipants[i]);
                    voiceParticipants.RemoveAt(i);
                }
            }
        }
    }

    public class Participant
    {
        public string Name { get; set; }
        public EndPoint SignallingAddress { get; set; }
        public EndPoint VoiceAddress { get; set; }

        public string LoginId { get; set; }
        public bool Binded { get; set; }
        public Vector3 Position { get; set; }
        public string EnvId { get; set; }

        public DateTime LastPing { get; set; } = DateTime.UtcNow;
    }
}
