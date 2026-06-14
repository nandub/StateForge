using System;

namespace StateForge.Replication
{
    /// <summary>Represents state forge witness state.</summary>
    public sealed class StateForgeWitnessState
    {
        /// <summary>Gets or sets the version.</summary>
        public string Version { get; set; }

        /// <summary>Gets or sets the witness name.</summary>
        public string WitnessName { get; set; }

        /// <summary>Gets or sets the witness root path.</summary>
        public string WitnessRootPath { get; set; }

        /// <summary>Gets or sets the last heartbeat utc.</summary>
        public DateTimeOffset LastHeartbeatUtc { get; set; }

        /// <summary>Gets or sets the candidate name.</summary>
        public string CandidateName { get; set; }

        /// <summary>Gets or sets the vote granted.</summary>
        public bool VoteGranted { get; set; }

        /// <summary>Gets or sets the last error.</summary>
        public string LastError { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeWitnessState"/> class.</summary>
        public StateForgeWitnessState()
        {
            Version = "1";
        }
    }
}
