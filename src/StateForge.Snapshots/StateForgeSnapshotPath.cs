using System;
using System.IO;

namespace StateForge.Snapshots
{
    internal static class StateForgeSnapshotPath
    {
        public static string ResolveChildName(string rootPath, string name, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(parameterName + " is required.", parameterName);
            }

            if (Path.IsPathRooted(name) ||
                name.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
                name.IndexOf(Path.AltDirectorySeparatorChar) >= 0 ||
                string.Equals(name, ".", StringComparison.Ordinal) ||
                string.Equals(name, "..", StringComparison.Ordinal))
            {
                throw new ArgumentException(parameterName + " must be a single directory name.", parameterName);
            }

            return ResolveContainedPath(rootPath, name, parameterName);
        }

        public static string ResolveRelativePath(string rootPath, string relativePath, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            {
                throw new InvalidDataException(parameterName + " must be a non-rooted relative path.");
            }

            return ResolveContainedPath(rootPath, relativePath, parameterName);
        }

        private static string ResolveContainedPath(string rootPath, string relativePath, string parameterName)
        {
            string root = Path.GetFullPath(rootPath);
            string candidate = Path.GetFullPath(Path.Combine(root, relativePath));
            string prefix = root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                root.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? root
                : root + Path.DirectorySeparatorChar;

            if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(parameterName + " resolves outside the allowed root.");
            }

            return candidate;
        }
    }
}
