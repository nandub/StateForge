using System.Diagnostics.Tracing;

namespace StateForge.Telemetry
{
    [EventSource(Name = "StateForge")]
    public sealed class StateForgeEventSource : EventSource
    {
        public static readonly StateForgeEventSource Log = new StateForgeEventSource();

        private StateForgeEventSource()
        {
        }

        [Event(1, Level = EventLevel.Informational, Message = "StateForge session read.")]
        public void SessionRead()
        {
            if (IsEnabled())
            {
                WriteEvent(1);
            }
        }

        [Event(2, Level = EventLevel.Informational, Message = "StateForge session written.")]
        public void SessionWritten()
        {
            if (IsEnabled())
            {
                WriteEvent(2);
            }
        }

        [Event(3, Level = EventLevel.Informational, Message = "StateForge session deleted.")]
        public void SessionDeleted()
        {
            if (IsEnabled())
            {
                WriteEvent(3);
            }
        }

        [Event(4, Level = EventLevel.Informational, Message = "StateForge lock acquired.")]
        public void LockAcquired()
        {
            if (IsEnabled())
            {
                WriteEvent(4);
            }
        }

        [Event(5, Level = EventLevel.Warning, Message = "StateForge lock contention detected.")]
        public void LockContention()
        {
            if (IsEnabled())
            {
                WriteEvent(5);
            }
        }

        [Event(6, Level = EventLevel.Informational, Message = "StateForge cleanup completed.")]
        public void CleanupCompleted()
        {
            if (IsEnabled())
            {
                WriteEvent(6);
            }
        }

        [Event(7, Level = EventLevel.Warning, Message = "StateForge file quarantined.")]
        public void FileQuarantined()
        {
            if (IsEnabled())
            {
                WriteEvent(7);
            }
        }

        [Event(8, Level = EventLevel.Error, Message = "StateForge corruption detected.")]
        public void CorruptionDetected()
        {
            if (IsEnabled())
            {
                WriteEvent(8);
            }
        }

        [Event(9, Level = EventLevel.Warning, Message = "StateForge health check failed: {0}")]
        public void HealthCheckFailed(string message)
        {
            if (IsEnabled())
            {
                WriteEvent(9, message ?? string.Empty);
            }
        }

        [Event(10, Level = EventLevel.Informational, Message = "StateForge health check passed.")]
        public void HealthCheckPassed()
        {
            if (IsEnabled())
            {
                WriteEvent(10);
            }
        }
    }
}
