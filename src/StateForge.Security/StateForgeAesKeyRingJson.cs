using System;
using System.Globalization;
using System.Text;

namespace StateForge.Security
{
    public static class StateForgeAesKeyRingJson
    {
        public static string ToJson(StateForgeAesKeyRing ring)
        {
            if (ring == null)
            {
                throw new ArgumentNullException("ring");
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.Append("  \"version\": \"").Append(Escape(ring.Version)).AppendLine("\",");
            builder.Append("  \"currentKeyId\": \"").Append(Escape(ring.CurrentKeyId)).AppendLine("\",");
            builder.AppendLine("  \"keys\": [");

            for (int i = 0; i < ring.Keys.Count; i++)
            {
                StateForgeAesKeyInfo key = ring.Keys[i];

                builder.AppendLine("    {");
                builder.Append("      \"keyId\": \"").Append(Escape(key.KeyId)).AppendLine("\",");
                builder.Append("      \"keyBase64\": \"").Append(Escape(key.KeyBase64)).AppendLine("\",");
                builder.Append("      \"createdUtc\": \"").Append(key.CreatedUtc.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)).AppendLine("\",");

                if (key.NotBeforeUtc.HasValue)
                {
                    builder.Append("      \"notBeforeUtc\": \"").Append(key.NotBeforeUtc.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)).AppendLine("\",");
                }
                else
                {
                    builder.AppendLine("      \"notBeforeUtc\": null,");
                }

                if (key.RetiredUtc.HasValue)
                {
                    builder.Append("      \"retiredUtc\": \"").Append(key.RetiredUtc.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture)).AppendLine("\"");
                }
                else
                {
                    builder.AppendLine("      \"retiredUtc\": null");
                }

                builder.Append("    }");

                if (i < ring.Keys.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
