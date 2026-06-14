using System;

namespace StateForge.Telemetry
{
    /// <summary>Isolates optional telemetry callbacks from StateForge runtime behavior.</summary>
    public static class StateForgeTelemetryScope
    {
        /// <summary>Invokes a telemetry callback and suppresses any exception it throws.</summary>
        /// <param name="action">The optional telemetry callback.</param>
        /// <remarks>Use this only for non-critical telemetry. Application and store behavior must not depend on the callback.</remarks>
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
