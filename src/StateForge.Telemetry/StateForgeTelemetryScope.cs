using System;

namespace StateForge.Telemetry
{
    public static class StateForgeTelemetryScope
    {
        public static void SafeRecord(Action action)
        {
            try
            {
                if (action != null)
                {
                    action();
                }
            }
            catch
            {
                // Telemetry must never affect runtime behavior.
            }
        }
    }
}
