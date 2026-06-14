using System;
using System.IO;
using StateForge.Format;

namespace StateForge.FileStore
{
    /// <summary>Migrates individual opaque record files into STFG2 envelopes.</summary>
    public static class StateForgeStfg2Migrator
    {
        /// <summary>Copies an STFG2 file or wraps legacy bytes into a new STFG2 destination.</summary>
        /// <param name="sourcePath">The existing record path.</param>
        /// <param name="destinationPath">The destination record path.</param>
        /// <param name="keyId">The optional key identifier for newly wrapped legacy bytes.</param>
        /// <param name="overwrite"><see langword="true"/> to replace an existing destination.</param>
        /// <returns>Source-format, migration, path, and length details.</returns>
        /// <exception cref="ArgumentException"><paramref name="sourcePath"/> or <paramref name="destinationPath"/> is blank.</exception>
        /// <exception cref="IOException">The destination exists and <paramref name="overwrite"/> is <see langword="false"/>.</exception>
        /// <remarks>
        /// Legacy source bytes are wrapped as an opaque payload. This operation does not decode or
        /// transform the underlying STFG1 record.
        /// </remarks>
        /// <example>
        /// Migrate one legacy record while preserving the source:
        /// <code language="csharp">
        /// StateForgeStfg2MigrationResult result = StateForgeStfg2Migrator.MigrateFile(
        ///     @"C:\StateForge\legacy.stfg",
        ///     @"C:\StateForge\migrated.stfg",
        ///     "key-002",
        ///     false);
        ///
        /// Console.WriteLine(result.Migrated);
        /// </code>
        /// </example>
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
