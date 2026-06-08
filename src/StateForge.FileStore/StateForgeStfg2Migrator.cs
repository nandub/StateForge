using System;
using System.IO;
using StateForge.Format;

namespace StateForge.FileStore
{
    public static class StateForgeStfg2Migrator
    {
        public static StateForgeStfg2MigrationResult MigrateFile(
            string sourcePath,
            string destinationPath,
            string keyId,
            bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException("Source path is required.", "sourcePath");
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("Destination path is required.", "destinationPath");
            }

            byte[] sourceBytes = File.ReadAllBytes(sourcePath);
            bool isStfg2 = StateForgeStfg2.IsStfg2(sourceBytes);

            if (File.Exists(destinationPath) && !overwrite)
            {
                throw new IOException("Destination already exists: " + destinationPath);
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath));

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            byte[] outputBytes;

            if (isStfg2)
            {
                outputBytes = sourceBytes;
            }
            else
            {
                outputBytes = StateForgeStfg2Envelope.Wrap(
                    sourceBytes,
                    false,
                    false,
                    false,
                    false,
                    keyId);
            }

            File.WriteAllBytes(destinationPath, outputBytes);

            StateForgeStfg2MigrationResult result = new StateForgeStfg2MigrationResult();
            result.SourcePath = sourcePath;
            result.DestinationPath = destinationPath;
            result.SourceWasStfg2 = isStfg2;
            result.Migrated = !isStfg2;
            result.KeyId = keyId;
            result.OriginalLength = sourceBytes.Length;
            result.NewLength = outputBytes.Length;

            return result;
        }
    }
}
