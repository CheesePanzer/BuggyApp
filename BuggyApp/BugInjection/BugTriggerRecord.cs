namespace BuggyApp.BugInjection;

public sealed record BugTriggerRecord(
    long Sequence,
    DateTimeOffset Timestamp,
    Guid SessionId,
    string BugId,
    BugScenario Scenario,
    string Page,
    string Operation,
    string? Detail);
