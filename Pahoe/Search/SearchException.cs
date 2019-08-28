namespace Pahoe.Search
{
    public readonly struct SearchException
    {
        public readonly string Message;
        public readonly string Severity;

        internal SearchException(string message, string severity)
        {
            Message = message;
            Severity = severity;
        }
    }
}
