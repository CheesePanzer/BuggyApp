namespace BuggyApp.BugInjection;

public interface IBugInjectionManager
{
    BugExperimentSession Session { get; }

    IReadOnlyList<BugDefinition> Definitions { get; }

    BugScenario EnabledScenarios { get; }

    BugTriggerMode TriggerMode { get; }

    bool IsScenarioEnabled(BugScenario scenario);

    void SetScenarioEnabled(BugScenario scenario, bool enabled);

    void EnableAll();

    void DisableAll();

    void SetTriggerMode(BugTriggerMode mode);

    bool IsEnabled(string bugId);

    BugDefinition GetDefinition(string bugId);

    bool TryTrigger(string bugId, string? detail = null);

    void RecordTriggered(string bugId, string? detail = null);

    IReadOnlyList<BugTriggerRecord> GetTriggeredSnapshot();

    void ClearTriggeredRecords();
}
