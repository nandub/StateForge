using System;
using StateForge.Prometheus;

namespace StateForge.PrometheusTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                StateForgePrometheusSnapshot snapshot = new StateForgePrometheusSnapshot();
                snapshot.Reads = 10;
                snapshot.Writes = 5;
                snapshot.SessionsActive = 3;
                snapshot.SessionsLocked = 1;

                string text = StateForgePrometheusFormatter.Format(snapshot);

                Require(text.Contains("# TYPE stateforge_reads_total counter"), "reads type missing");
                Require(text.Contains("stateforge_reads_total 10"), "reads value missing");
                Require(text.Contains("stateforge_writes_total 5"), "writes value missing");
                Require(text.Contains("# TYPE stateforge_sessions_active gauge"), "active gauge type missing");
                Require(text.Contains("stateforge_sessions_locked 1"), "locked value missing");

                Console.WriteLine("PASS: Prometheus reads metric");
                Console.WriteLine("PASS: Prometheus writes metric");
                Console.WriteLine("PASS: Prometheus session gauge");
                Console.WriteLine("PASS: Prometheus text format");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
