namespace BuggyApp.BugInjection;

public sealed class BugInjectionOptions
{
    public string BuildLabel { get; init; } = "Buggy";

    // Infrastructure is inert until scenarios are explicitly enabled.
    public BugScenario EnabledScenarios { get; init; } = BugScenario.None;

    public IReadOnlySet<string> DisabledBugIds { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public bool CrashOnTriggeredBug { get; init; } = true;

    public BugTriggerMode TriggerMode { get; init; } = BugTriggerMode.Natural;

    public int UiThreadBlockMilliseconds { get; init; } = 8000;

    public bool IsEnabled(BugDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return EnabledScenarios.HasFlag(definition.Scenario)
            && !DisabledBugIds.Contains(definition.Id);
    }
}
