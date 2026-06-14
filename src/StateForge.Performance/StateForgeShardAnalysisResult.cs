using System.Collections.Generic;

namespace StateForge.Performance
{
    /// <summary>Describes the observed file distribution of a StateForge session store.</summary>
    public sealed class StateForgeShardAnalysisResult
    {
        /// <summary>Gets or sets the analyzed root path.</summary>
        public string RootPath { get; set; }
        /// <summary>Gets or sets the directory in which session files were analyzed.</summary>
        public string SessionsPath { get; set; }
        /// <summary>Gets or sets the number of immediate shard directories.</summary>
        public int DirectoryCount { get; set; }
        /// <summary>Gets or sets the total number of STFG session files.</summary>
        public int FileCount { get; set; }
        /// <summary>Gets or sets the largest number of files found in one shard directory.</summary>
        public int MaxFilesPerDirectory { get; set; }
        /// <summary>Gets or sets the smallest number of files found in one shard directory.</summary>
        public int MinFilesPerDirectory { get; set; }
        /// <summary>Gets or sets the average number of files per shard directory.</summary>
        public double AverageFilesPerDirectory { get; set; }
        /// <summary>Gets or sets whether the observed layout appears fully sharded.</summary>
        public bool AppearsSharded { get; set; }
        /// <summary>Gets warnings about incomplete or potentially overloaded shard layouts.</summary>
        public List<string> Warnings { get; private set; }

        /// <summary>Initializes an empty shard analysis result.</summary>
        public StateForgeShardAnalysisResult()
        {
            Warnings = new List<string>();
        }
    }
}
