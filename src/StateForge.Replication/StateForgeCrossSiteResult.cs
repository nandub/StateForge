using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge cross site result.</summary>
    public sealed class StateForgeCrossSiteResult
    {
        /// <summary>Gets or sets the eligible.</summary>
        public bool Eligible { get; set; }

        /// <summary>Gets or sets the source site name.</summary>
        public string SourceSiteName { get; set; }

        /// <summary>Gets or sets the target site name.</summary>
        public string TargetSiteName { get; set; }

        /// <summary>Gets or sets the target root path.</summary>
        public string TargetRootPath { get; set; }

        /// <summary>Gets or sets the candidate name.</summary>
        public string CandidateName { get; set; }

        /// <summary>Gets the reasons.</summary>
        public List<string> Reasons { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeCrossSiteResult"/> class.</summary>
        public StateForgeCrossSiteResult()
        {
            Reasons = new List<string>();
        }
    }
}
