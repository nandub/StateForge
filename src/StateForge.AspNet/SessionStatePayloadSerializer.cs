using System.IO;
using System.Web.SessionState;

namespace StateForge.AspNet
{
    internal static class SessionStatePayloadSerializer
    {
        public static byte[] Serialize(SessionStateItemCollection items)
        {
            if (items == null) { items = new SessionStateItemCollection(); }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                items.Serialize(writer);
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static SessionStateItemCollection Deserialize(byte[] value)
        {
            if (value == null || value.Length == 0) { return new SessionStateItemCollection(); }

            using (MemoryStream stream = new MemoryStream(value))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                return SessionStateItemCollection.Deserialize(reader);
            }
        }
    }
}
