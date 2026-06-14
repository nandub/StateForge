using System;
using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge witness health entry.</summary>
    public sealed class StateForgeWitnessHealthEntry
    {
        /// <summary>Gets or sets the witness name.</summary>
        public string WitnessName { get; set; }

        /// <summary>Gets or sets the witness root path.</summary>
        public string WitnessRootPath { get; set; }

        /// <summary>Gets or sets the last heartbeat utc.</summary>
        public DateTimeOffset? LastHeartbeatUtc { get; set; }

        /// <summary>Gets or sets the age seconds.</summary>
        public double AgeSeconds { get; set; }

        /// <summary>Gets or sets the healthy.</summary>
        public bool Healthy { get; set; }

        /// <summary>Gets or sets the vote granted.</summary>
        public bool VoteGranted { get; set; }

        /// <summary>Gets or sets the vote counted.</summary>
        public bool VoteCounted { get; set; }

        /// <summary>Gets or sets the candidate name.</summary>
        public string CandidateName { get; set; }

        /// <summary>Gets the reasons.</summary>
        public List<string> Reasons { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeWitnessHealthEntry"/> class.</summary>
        public StateForgeWitnessHealthEntry()
        {
            Reasons = new List<string>();
        }
    }
}
