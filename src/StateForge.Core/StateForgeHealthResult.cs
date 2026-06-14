using System.Collections.Generic;

namespace StateForge.Core
{
    /// <summary>Reports the capabilities verified by a store health probe.</summary>
    public sealed class StateForgeHealthResult
    {
        private readonly List<string> _errors;

        /// <summary>Initializes an empty health result.</summary>
        public StateForgeHealthResult()
        {
            _errors = new List<string>();
        }

        /// <summary>Gets a value indicating whether every capability passed and no errors were recorded.</summary>
        public bool Healthy
        {
            get { return _errors.Count == 0 && CanRead && CanWrite && CanLock && CanEnumerate && CanCleanup; }
        }

        /// <summary>Gets or sets a value indicating whether reads succeeded.</summary>
        public bool CanRead { get; set; }
        /// <summary>Gets or sets a value indicating whether writes succeeded.</summary>
        public bool CanWrite { get; set; }
        /// <summary>Gets or sets a value indicating whether locking succeeded.</summary>
        public bool CanLock { get; set; }
        /// <summary>Gets or sets a value indicating whether enumeration succeeded.</summary>
        public bool CanEnumerate { get; set; }
        /// <summary>Gets or sets a value indicating whether cleanup succeeded.</summary>
        public bool CanCleanup { get; set; }

        /// <summary>Gets the mutable list of health errors.</summary>
        public IList<string> Errors
        {
            get { return _errors; }
        }

        /// <summary>Adds an error to the health result.</summary>
        /// <param name="message">The error description.</param>
        public void AddError(string message)
        {
            _errors.Add(message);
        }
    }
}
