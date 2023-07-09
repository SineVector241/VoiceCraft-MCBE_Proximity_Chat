namespace VoiceCraft.Server.Helpers
{
    public static class ServerEvents
    {
        public delegate Task ParticipantLogin(Participant participant, ushort Key, string? reason = null);
        public delegate Task ParticipantLogout(Participant participant, ushort Key, string? reason = null);
        public delegate Task ParticipantBinded(Participant participant, ushort Key);
        public delegate Task Failed(string service, string reason);
        public delegate Task Started(string service);
        public delegate Task Stopping();

        //Events
        public static event ParticipantLogin? OnParticipantLogin;
        public static event ParticipantLogout? OnParticipantLogout;
        public static event ParticipantBinded? OnParticipantBinded;
        public static event Failed? OnFailed;
        public static event Started? OnStarted;
        public static event Stopping? OnStopping;

        public static void InvokeParticipantLogin(Participant Participant, ushort Key, string? reason = null)
        {
            Logger.LogToConsole(LogType.Success, $"Added Client - Key: {Key}.", nameof(ServerEvents));
            var eventObject = OnParticipantLogin;
            eventObject?.Invoke(Participant, Key, reason);
        }

        public static void InvokeParticipantLogout(Participant Participant, ushort Key, string? reason = null)
        {
            Logger.LogToConsole(LogType.Warn, $"Removed Client - Gamertag: {Participant.MinecraftData.Gamertag} Key:{Key} Reason: {reason}.", nameof(ServerEvents));
            var eventObject = OnParticipantLogout;
            eventObject?.Invoke(Participant, Key, reason);
        }

        public static void InvokeParticipantBinded(Participant Participant, ushort Key)
        {
            Logger.LogToConsole(LogType.Success, $"Successfully binded participant: Gamertag: {Participant.MinecraftData.Gamertag}, Key: {Key}.", nameof(ServerEvents));
            var eventObject = OnParticipantBinded;
            eventObject?.Invoke(Participant, Key);
        }

        public static void InvokeFailed(string Service, string Reason)
        {
            Logger.LogToConsole(LogType.Error, $"{Service} failed to start. Reason: {Reason}.", nameof(ServerEvents));
            var eventObject = OnFailed;
            eventObject?.Invoke(Service, Reason);
        }

        public static void InvokeStarted(string Service)
        {
            Logger.LogToConsole(LogType.Success, $"{Service} successfully started.", nameof(ServerEvents));
            var eventObject = OnStarted;
            eventObject?.Invoke(Service);
        }

        public static void InvokeStopping()
        {
            Logger.LogToConsole(LogType.Success, "Stopping server.", nameof(ServerEvents));
            var eventObject = OnStopping;
            eventObject?.Invoke();
        }
    }
}
