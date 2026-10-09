namespace BuggyApp.BugInjection;

public sealed record BugExperimentSession(
    Guid SessionId,
    DateTimeOffset StartedAt,
    string BuildLabel,
    BugScenario EnabledScenarios)
{
    public static BugExperimentSession Start(BugInjectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new BugExperimentSession(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            options.BuildLabel,
            options.EnabledScenarios);
    }
}
