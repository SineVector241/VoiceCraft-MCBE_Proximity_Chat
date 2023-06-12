using System.Collections.Concurrent;
using System.Net;
using System.Numerics;

namespace VoiceCraft.Server.Helpers
{
    public static class ServerData
    {
        public static ConcurrentDictionary<ushort, Participant> Participants { get; } = new ConcurrentDictionary<ushort, Participant>();
        private static PeriodicTimer? Timer = null;

        public static async void StartTimer(CancellationToken CT)
        {
            try
            {
                Timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
                while (await Timer.WaitForNextTickAsync(CT))
                {
                    foreach (var key in Participants.Keys)
                    {
                        Participants.TryGetValue(key, out Participant? participant);
                        if (participant != null && DateTime.UtcNow.Subtract(participant.SocketData.LastPing).TotalSeconds > 10)
                        {
                            RemoveParticipant(key, "TimeOut");
                        }
                    }
                }
            }
            catch(OperationCanceledException) 
            {
                Timer?.Dispose();
                Timer = null;
            }
            catch(Exception ex)
            {
                Logger.LogToConsole(LogType.Error, ex.Message, nameof(ServerData));
            }
        }

        public static bool AddParticipant(ushort Key, Participant Participant)
        {
            var res = Participants.TryAdd(Key, Participant);

            if (res)
                ServerEvents.InvokeParticipantLogin(Participant, Key);
            return res;
        }

        public static bool RemoveParticipant(ushort Key, string? Reason = null)
        {
            var res = Participants.TryRemove(Key, out Participant? participant);

            if(res && participant != null)
                ServerEvents.InvokeParticipantLogout(participant, Key, Reason);
            return res;
        }

        public static Participant? GetParticipantByKey(ushort Id)
        {
            Participants.TryGetValue(Id, out Participant? participant);
            return participant;
        }

        public static Participant? GetParticipantByMinecraftId(string Id)
        {
            foreach (var value in Participants.Values)
            {
                if (value != null && value.MinecraftData.PlayerId == Id)
                    return value;
            }
            return null;
        }

        public static Participant? GetParticipantBySignalling(EndPoint EndPoint)
        {
            foreach(var value in Participants.Values)
            {
                if (value != null && value.SocketData.SignallingAddress?.ToString() == EndPoint.ToString())
                    return value;
            }
            return null;
        }

        public static Participant? GetParticipantByVoice(EndPoint EndPoint)
        {
            foreach (var value in Participants.Values)
            {
                if (value != null && value.SocketData.VoiceAddress?.ToString() == EndPoint.ToString())
                    return value;
            }
            return null;
        }
    }

    public class Participant
    {
        public bool Binded { get; set; }
        public bool Muted { get; set; }
        public bool ClientSided { get; set; }
        public AudioCodecs Codec { get; set; }

        public MinecraftData MinecraftData { get; set; } = new MinecraftData();
        public SocketData SocketData { get; set; } = new SocketData();
    }

    public class MinecraftData
    {
        public string Gamertag { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public string DimensionId { get; set; } = "void";
    }

    public class SocketData
    {
        public EndPoint? SignallingAddress { get; set; }
        public EndPoint? VoiceAddress { get; set; }
        public DateTime LastPing { get; set; } = DateTime.UtcNow.AddMinutes(5);
    }
}
