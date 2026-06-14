using System.Collections.Generic;
using System.Diagnostics;

namespace StateForge.Telemetry
{
    /// <summary>Publishes lightweight StateForge diagnostic events through <see cref="DiagnosticListener"/>.</summary>
    public static class StateForgeDiagnosticSource
    {
        /// <summary>Gets the process-wide StateForge diagnostic listener.</summary>
        public static readonly DiagnosticListener Listener = new DiagnosticListener("StateForge");

        /// <summary>Writes a named event when a subscriber has enabled it.</summary>
        /// <param name="name">The diagnostic event name.</param>
        /// <param name="payload">The event payload.</param>
        public static void Write(string name, object payload)
        {
            if (Listener.IsEnabled(name))
            {
                Listener.Write(name, payload);
            }
        }

        /// <summary>Writes a standard <c>StateForge.Operation</c> event.</summary>
        /// <param name="operation">The operation name.</param>
        /// <param name="key">The affected StateForge key.</param>
        public static void WriteOperation(string operation, string key)
        {
            if (Listener.IsEnabled("StateForge.Operation"))
            {
                Dictionary<string, object> payload = new Dictionary<string, object>();
                payload["operation"] = operation;
                payload["key"] = key;
                Listener.Write("StateForge.Operation", payload);
            }
        }
    }
}
