using System;

namespace StateForge.Replication
{
    public sealed class StateForgeWitnessState
    {
        public string Version { get; set; }

        public string WitnessName { get; set; }

        public string WitnessRootPath { get; set; }

        public DateTimeOffset LastHeartbeatUtc { get; set; }

        public string CandidateName { get; set; }

        public bool VoteGranted { get; set; }

        public string LastError { get; set; }

        public StateForgeWitnessState()
        {
            Version = "1";
        }
    }
}
