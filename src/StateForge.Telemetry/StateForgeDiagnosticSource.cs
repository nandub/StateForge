using System.Collections.Generic;
using System.Diagnostics;

namespace StateForge.Telemetry
{
    public static class StateForgeDiagnosticSource
    {
        public static readonly DiagnosticListener Listener = new DiagnosticListener("StateForge");

        public static void Write(string name, object payload)
        {
            if (Listener.IsEnabled(name))
            {
                Listener.Write(name, payload);
            }
        }

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
