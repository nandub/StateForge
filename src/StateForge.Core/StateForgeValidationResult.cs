using System.Collections.Generic;

namespace StateForge.Core
{
    public sealed class StateForgeValidationResult
    {
        private readonly List<string> _errors;
        private readonly List<string> _warnings;

        public StateForgeValidationResult()
        {
            _errors = new List<string>();
            _warnings = new List<string>();
        }

        public bool Success
        {
            get { return _errors.Count == 0; }
        }

        public IList<string> Errors
        {
            get { return _errors; }
        }

        public IList<string> Warnings
        {
            get { return _warnings; }
        }

        public void AddError(string message)
        {
            _errors.Add(message);
        }

        public void AddWarning(string message)
        {
            _warnings.Add(message);
        }
    }
}
