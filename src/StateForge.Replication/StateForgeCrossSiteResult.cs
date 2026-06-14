using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeCrossSiteResult
    {
        public bool Eligible { get; set; }

        public string SourceSiteName { get; set; }

        public string TargetSiteName { get; set; }

        public string TargetRootPath { get; set; }

        public string CandidateName { get; set; }

        public List<string> Reasons { get; private set; }

        public StateForgeCrossSiteResult()
        {
            Reasons = new List<string>();
        }
    }
}
