using System.Threading.Tasks;
using VoiceCraft_Server.Servers;

namespace VoiceCraft_Server.Data
{
    public static class ServerEvents
    {
        public delegate Task ParticipantLogin(Participant participant, string reason = null);
        public delegate Task ParticipantLogout(Participant participant, string reason = null);
        public delegate Task ParticipantBinded(Participant participant);
        public delegate Task Failed (string service, string reason);
        public delegate Task Started (string service);

        //Events
        public static event ParticipantLogin OnParticipantLogin;
        public static event ParticipantLogout OnParticipantLogout;
        public static event ParticipantBinded OnParticipantBinded;
        public static event Failed OnFailed;
        public static event Started OnStarted;

        public static void InvokeParticipantLogin(Participant participant, string reason = null)
        {
            var eventObject = OnParticipantLogin;
            eventObject?.Invoke(participant, reason);
            Logger.LogToConsole(LogType.Success, $"Added Client - Key: {participant.LoginKey}", nameof(ServerEvents));
        }

        public static void InvokeParticipantLogout(Participant participant, string reason = null)
        {
            Logger.LogToConsole(LogType.Warn, $"Removed Client - Gamertag: {participant.MinecraftData.Gamertag} Key:{participant.LoginKey} Reason: {reason}", nameof(ServerEvents));
            var eventObject = OnParticipantLogout;
            eventObject?.Invoke(participant, reason);
        }

        public static void InvokeParticipantBinded(Participant participant)
        {
            Logger.LogToConsole(LogType.Success, $"Successfully binded user: Gamertag: {participant.MinecraftData.Gamertag}, Key: {participant.LoginKey}", nameof(ServerEvents));
            var eventObject = OnParticipantBinded;
            eventObject.Invoke(participant);
        }

        public static void InvokeFailed(string service, string reason)
        {
            Logger.LogToConsole(LogType.Error, $"{service} failed to start. Reason: {reason}", nameof(ServerEvents));
            var eventObject = OnFailed;
            eventObject?.Invoke(service, reason);
        }

        public static void InvokeStarted(string service)
        {
            Logger.LogToConsole(LogType.Success, $"{service} successfully started", nameof(ServerEvents));
            var eventObject = OnStarted;
            eventObject?.Invoke(service);
        }
    }
}
