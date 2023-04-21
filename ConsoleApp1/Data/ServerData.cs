using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading;

namespace VoiceCraft_Server.Data
{
    public class ServerData
    {
        private List<Participant> Participants;
        private Timer Timer;

        public ServerData() 
        {
            Participants = new List<Participant>();
        }

        public void Start()
        {
            Timer = new Timer(new TimerCallback(CheckParticipants), null, 5000, 5000);
        }


        //Getting participants
        public List<Participant> GetParticipants()
        {
            return Participants;
        }

        public Participant GetParticipantByMinecraftId(string id)
        {
            var participant = Participants.FirstOrDefault(x => x.MinecraftData.PlayerId == id);
            return participant;
        }

        public Participant GetParticipantByKey(string loginKey)
        {
            var participant = Participants.FirstOrDefault(x => x.LoginKey == loginKey);
            return participant;
        }

        public Participant GetParticipantByVoiceAddress(EndPoint endPoint)
        {
            var participant = Participants.FirstOrDefault(x => x.SocketData.VoiceAddress?.ToString() == endPoint.ToString());
            return participant;
        }

        public Participant GetParticipantBySignallingAddress(EndPoint endPoint)
        {
            var participant = Participants.FirstOrDefault(x => x.SocketData.SignallingAddress?.ToString() == endPoint.ToString());
            return participant;
        }

        //Adding, Editing or Removing participants
        public bool EditParticipant(Participant participant)
        {
            var participantIndex = Participants.FindIndex(x => x.LoginKey == participant.LoginKey);
            if (participantIndex == -1)
                return false;

            Participants[participantIndex] = participant;

            return true;
        }

        public void AddParticipant(Participant participant)
        {
            Participants.Add(participant);
            ServerEvents.InvokeParticipantLogin(participant);
        }

        public void RemoveParticipant(Participant participant, bool sendToClient)
        {
            Participants.RemoveAll(x => x.LoginKey == participant.LoginKey);
            ServerEvents.InvokeParticipantLogout(participant, sendToClient? "Server Request" : null);
        }

        //Check Participants
        public void CheckParticipants(object state)
        {
            for (int i = 0; i < Participants.Count; i++)
            {
                if (DateTime.UtcNow.Subtract(Participants[i].SocketData.LastPing).Seconds > 10)
                {
                    ServerEvents.InvokeParticipantLogout(Participants[i], "Timed Out");
                    Participants.RemoveAt(i);
                }
            }
        }
    }

    public class Participant
    {
        public string LoginKey { get; set; }
        public bool Binded { get; set; }

        public MinecraftData MinecraftData { get; set; } = new MinecraftData();
        public SocketData SocketData { get; set; } = new SocketData();
    }

    public class MinecraftData
    {
        public string Gamertag { get; set; }
        public string PlayerId { get; set; }
        public Vector3 Position { get; set; }
        public string DimensionId { get; set; } = "void";
    }

    public class SocketData
    {
        public EndPoint SignallingAddress { get; set; }
        public EndPoint VoiceAddress { get; set; }
        public DateTime LastPing { get; set; } = DateTime.UtcNow;
    }
}
