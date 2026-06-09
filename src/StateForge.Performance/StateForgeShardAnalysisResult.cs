using System.Collections.Generic;

namespace StateForge.Performance
{
    public sealed class StateForgeShardAnalysisResult
    {
        public string RootPath { get; set; }
        public string SessionsPath { get; set; }
        public int DirectoryCount { get; set; }
        public int FileCount { get; set; }
        public int MaxFilesPerDirectory { get; set; }
        public int MinFilesPerDirectory { get; set; }
        public double AverageFilesPerDirectory { get; set; }
        public bool AppearsSharded { get; set; }
        public List<string> Warnings { get; private set; }

        public StateForgeShardAnalysisResult()
        {
            Warnings = new List<string>();
        }
    }
}
