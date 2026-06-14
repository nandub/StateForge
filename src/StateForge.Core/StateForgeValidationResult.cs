using System.Collections.Generic;

namespace StateForge.Core
{
    /// <summary>Collects configuration validation errors and warnings.</summary>
    public sealed class StateForgeValidationResult
    {
        private readonly List<string> _errors;
        private readonly List<string> _warnings;

        /// <summary>Initializes an empty validation result.</summary>
        public StateForgeValidationResult()
        {
            _errors = new List<string>();
            _warnings = new List<string>();
        }

        /// <summary>Gets a value indicating whether no validation errors were recorded.</summary>
        public bool Success
        {
            get { return _errors.Count == 0; }
        }

        /// <summary>Gets the mutable list of validation errors.</summary>
        public IList<string> Errors
        {
            get { return _errors; }
        }

        /// <summary>Gets the mutable list of validation warnings.</summary>
        public IList<string> Warnings
        {
            get { return _warnings; }
        }

        /// <summary>Adds a validation error.</summary>
        /// <param name="message">The error description.</param>
        public void AddError(string message)
        {
            _errors.Add(message);
        }

        /// <summary>Adds a validation warning.</summary>
        /// <param name="message">The warning description.</param>
        public void AddWarning(string message)
        {
            _warnings.Add(message);
        }
    }
}
