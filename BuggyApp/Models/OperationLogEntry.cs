namespace BuggyApp.Models;

public enum OperationStatus
{
    Started,
    Succeeded,
    Cancelled,
    Failed
}

public sealed record OperationLogEntry(
    long Sequence,
    DateTimeOffset Timestamp,
    string Operation,
    OperationStatus Status,
    TimeSpan Elapsed,
    string? Detail = null);
