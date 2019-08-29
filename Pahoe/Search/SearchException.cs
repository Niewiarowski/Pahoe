using System;

namespace Pahoe.Search
{
    public sealed class SearchException : Exception
    {
        public string Severity { get; }

        internal SearchException(string message, string severity) : base(message)
        {
            Severity = severity;
        }

        public override string ToString()
            => string.Format("{0}: {1}", Severity, Message);
    }
}
