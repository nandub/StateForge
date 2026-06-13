using System;
using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeWitnessHealthEntry
    {
        public string WitnessName { get; set; }

        public string WitnessRootPath { get; set; }

        public DateTimeOffset? LastHeartbeatUtc { get; set; }

        public double AgeSeconds { get; set; }

        public bool Healthy { get; set; }

        public bool VoteGranted { get; set; }

        public bool VoteCounted { get; set; }

        public string CandidateName { get; set; }

        public List<string> Reasons { get; private set; }

        public StateForgeWitnessHealthEntry()
        {
            Reasons = new List<string>();
        }
    }
}
