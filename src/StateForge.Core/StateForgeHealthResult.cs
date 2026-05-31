using System.Collections.Generic;

namespace StateForge.Core
{
    public sealed class StateForgeHealthResult
    {
        private readonly List<string> _errors;

        public StateForgeHealthResult()
        {
            _errors = new List<string>();
        }

        public bool Healthy
        {
            get { return _errors.Count == 0 && CanRead && CanWrite && CanLock && CanEnumerate && CanCleanup; }
        }

        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanLock { get; set; }
        public bool CanEnumerate { get; set; }
        public bool CanCleanup { get; set; }

        public IList<string> Errors
        {
            get { return _errors; }
        }

        public void AddError(string message)
        {
            _errors.Add(message);
        }
    }
}
