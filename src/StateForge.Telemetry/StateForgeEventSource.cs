using System.Diagnostics.Tracing;

namespace StateForge.Telemetry
{
    /// <summary>Emits StateForge ETW and EventListener events.</summary>
    [EventSource(Name = "StateForge")]
    public sealed class StateForgeEventSource : EventSource
    {
        /// <summary>Gets the process-wide StateForge event source.</summary>
        public static readonly StateForgeEventSource Log = new StateForgeEventSource();

        private StateForgeEventSource()
        {
        }

        /// <summary>Emits the session-read event.</summary>
        [Event(1, Level = EventLevel.Informational, Message = "StateForge session read.")]
        public void SessionRead()
        {
            if (IsEnabled())
            {
                WriteEvent(1);
            }
        }

        /// <summary>Emits the session-written event.</summary>
        [Event(2, Level = EventLevel.Informational, Message = "StateForge session written.")]
        public void SessionWritten()
        {
            if (IsEnabled())
            {
                WriteEvent(2);
            }
        }

        /// <summary>Emits the session-deleted event.</summary>
        [Event(3, Level = EventLevel.Informational, Message = "StateForge session deleted.")]
        public void SessionDeleted()
        {
            if (IsEnabled())
            {
                WriteEvent(3);
            }
        }

        /// <summary>Emits the lock-acquired event.</summary>
        [Event(4, Level = EventLevel.Informational, Message = "StateForge lock acquired.")]
        public void LockAcquired()
        {
            if (IsEnabled())
            {
                WriteEvent(4);
            }
        }

        /// <summary>Emits the lock-contention warning event.</summary>
        [Event(5, Level = EventLevel.Warning, Message = "StateForge lock contention detected.")]
        public void LockContention()
        {
            if (IsEnabled())
            {
                WriteEvent(5);
            }
        }

        /// <summary>Emits the cleanup-completed event.</summary>
        [Event(6, Level = EventLevel.Informational, Message = "StateForge cleanup completed.")]
        public void CleanupCompleted()
        {
            if (IsEnabled())
            {
                WriteEvent(6);
            }
        }

        /// <summary>Emits the file-quarantined warning event.</summary>
        [Event(7, Level = EventLevel.Warning, Message = "StateForge file quarantined.")]
        public void FileQuarantined()
        {
            if (IsEnabled())
            {
                WriteEvent(7);
            }
        }

        /// <summary>Emits the corruption-detected error event.</summary>
        [Event(8, Level = EventLevel.Error, Message = "StateForge corruption detected.")]
        public void CorruptionDetected()
        {
            if (IsEnabled())
            {
                WriteEvent(8);
            }
        }

        /// <summary>Emits a health-check failure warning.</summary>
        /// <param name="message">The health-check failure description.</param>
        [Event(9, Level = EventLevel.Warning, Message = "StateForge health check failed: {0}")]
        public void HealthCheckFailed(string message)
        {
            if (IsEnabled())
            {
                WriteEvent(9, message ?? string.Empty);
            }
        }

        /// <summary>Emits the health-check-passed event.</summary>
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
