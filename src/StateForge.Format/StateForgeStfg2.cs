using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StateForge.Format
{
    public sealed class StateForgeStfg2ReadResult
    {
        public byte Version { get; set; }
        public StateForgeFormatFlags Flags { get; set; }
        public string KeyId { get; set; }
        public byte[] Checksum { get; set; }
        public byte[] Payload { get; set; }
        public bool ChecksumValid { get; set; }
    }

    public static class StateForgeStfg2
    {
        public const string Magic = "STFG";
        public const byte Version2 = 2;
        public const int Sha256Length = 32;

        public static byte[] Write(byte[] payload, StateForgeFormatFlags flags, string keyId)
        {
            if (payload == null) throw new ArgumentNullException("payload");

            byte[] keyBytes = Encoding.UTF8.GetBytes(keyId ?? string.Empty);

            if (keyBytes.Length > ushort.MaxValue)
            {
                throw new InvalidOperationException("KeyId is too long.");
            }

            byte[] checksum = Sha256(payload);

            using (MemoryStream stream = new MemoryStream())
            {
                byte[] magic = Encoding.ASCII.GetBytes(Magic);
                stream.Write(magic, 0, magic.Length);
                stream.WriteByte(Version2);
                WriteInt32(stream, (int)flags);
                WriteUInt16(stream, (ushort)keyBytes.Length);
                stream.Write(keyBytes, 0, keyBytes.Length);
                stream.Write(checksum, 0, checksum.Length);
                stream.Write(payload, 0, payload.Length);
                return stream.ToArray();
            }
        }

        public static StateForgeStfg2ReadResult Read(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");

            using (MemoryStream stream = new MemoryStream(bytes))
            {
                byte[] magicBytes = new byte[4];
                ReadExact(stream, magicBytes, 0, magicBytes.Length);

                string magic = Encoding.ASCII.GetString(magicBytes);
                if (!string.Equals(magic, Magic, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Invalid STFG magic.");
                }

                int version = stream.ReadByte();
                if (version != Version2)
                {
                    throw new InvalidOperationException("Unsupported STFG version: " + version);
                }

                StateForgeFormatFlags flags = (StateForgeFormatFlags)ReadInt32(stream);
                ushort keyLength = ReadUInt16(stream);

                byte[] keyBytes = new byte[keyLength];
                ReadExact(stream, keyBytes, 0, keyBytes.Length);
                string keyId = Encoding.UTF8.GetString(keyBytes);

                byte[] checksum = new byte[Sha256Length];
                ReadExact(stream, checksum, 0, checksum.Length);

                byte[] payload = new byte[stream.Length - stream.Position];
                ReadExact(stream, payload, 0, payload.Length);

                StateForgeStfg2ReadResult result = new StateForgeStfg2ReadResult();
                result.Version = Version2;
                result.Flags = flags;
                result.KeyId = keyId;
                result.Checksum = checksum;
                result.Payload = payload;
                result.ChecksumValid = ConstantTimeEquals(checksum, Sha256(payload));
                return result;
            }
        }

        public static bool IsStfg2(byte[] bytes)
        {
            return bytes != null &&
                bytes.Length >= 5 &&
                bytes[0] == (byte)'S' &&
                bytes[1] == (byte)'T' &&
                bytes[2] == (byte)'F' &&
                bytes[3] == (byte)'G' &&
                bytes[4] == Version2;
        }

        public static string ToHex(byte[] bytes)
        {
            if (bytes == null) return string.Empty;

            char[] chars = new char[bytes.Length * 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                chars[i * 2] = Hex(b / 16);
                chars[(i * 2) + 1] = Hex(b % 16);
            }

            return new string(chars);
        }

        private static byte[] Sha256(byte[] payload)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(payload);
            }
        }

        private static bool ConstantTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;

            int diff = 0;

            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }

        private static char Hex(int value)
        {
            return (char)(value < 10 ? value + 48 : value - 10 + 65);
        }

        private static void WriteInt32(Stream stream, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static int ReadInt32(Stream stream)
        {
            byte[] bytes = new byte[4];
            ReadExact(stream, bytes, 0, bytes.Length);
            return BitConverter.ToInt32(bytes, 0);
        }

        private static void WriteUInt16(Stream stream, ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static ushort ReadUInt16(Stream stream)
        {
            byte[] bytes = new byte[2];
            ReadExact(stream, bytes, 0, bytes.Length);
            return BitConverter.ToUInt16(bytes, 0);
        }

        private static void ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int total = 0;

            while (total < count)
            {
                int read = stream.Read(buffer, offset + total, count - total);
                if (read == 0) throw new EndOfStreamException();
                total += read;
            }
        }
    }
}
