namespace BuggyApp.BugInjection;

public sealed class InjectedBugException : Exception
{
    public InjectedBugException(string bugId, string message)
        : base($"[{bugId}] {message}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bugId);
        BugId = bugId;
    }

    public InjectedBugException(string bugId, string message, Exception innerException)
        : base($"[{bugId}] {message}", innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bugId);
        BugId = bugId;
    }

    public string BugId { get; }
}
