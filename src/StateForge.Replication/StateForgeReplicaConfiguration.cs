using System;
using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Provides state forge replica configuration operations.</summary>
    public static class StateForgeReplicaConfiguration
    {
        /// <summary>Performs the parse operation.</summary>
        public static List<StateForgeReplicaNode> Parse(string value)
        {
            List<StateForgeReplicaNode> replicas = new List<StateForgeReplicaNode>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return replicas;
            }

            string[] entries = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i].Trim();
                if (entry.Length == 0)
                {
                    continue;
                }

                string name;
                string rootPath;
                int separator = entry.IndexOf('=');
                if (separator >= 0)
                {
                    name = entry.Substring(0, separator).Trim();
                    rootPath = entry.Substring(separator + 1).Trim();
                    if (name.Length == 0 || rootPath.Length == 0)
                    {
                        throw new FormatException(
                            "Replica entries must use the format 'name=path' or 'path'.");
                    }
                }
                else
                {
                    name = "replica-" + (replicas.Count + 1);
                    rootPath = entry;
                }

                replicas.Add(new StateForgeReplicaNode
                {
                    Name = name,
                    RootPath = rootPath
                });
            }

            return replicas;
        }
    }
}
