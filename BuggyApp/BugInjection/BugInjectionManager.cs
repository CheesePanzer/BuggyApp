using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BuggyApp.BugInjection;

public sealed class BugInjectionManager : IBugInjectionManager
{
    private const int MaximumTriggerRecords = 2000;
    private readonly BugCatalog _catalog;
    private readonly BugInjectionOptions _options;
    private readonly ILogger<BugInjectionManager> _logger;
    private readonly ConcurrentQueue<BugTriggerRecord> _triggerRecords = new();
    private long _sequence;
    private int _enabledScenarios;
    private int _triggerMode;

    public BugInjectionManager(
        BugCatalog catalog,
        BugInjectionOptions options,
        ILogger<BugInjectionManager> logger)
    {
        _catalog = catalog;
        _options = options;
        _logger = logger;
        _enabledScenarios = (int)options.EnabledScenarios;
        _triggerMode = (int)options.TriggerMode;
        Session = BugExperimentSession.Start(options);
        Definitions = catalog.GetAll();

        _logger.LogInformation(
            BugInjectionLogEvents.SessionStarted,
            "Bug injection session started. SessionId={SessionId}, Build={BuildLabel}, EnabledScenarios={EnabledScenarios}, RegisteredBugs={RegisteredBugCount}",
            Session.SessionId,
            Session.BuildLabel,
            Session.EnabledScenarios,
            Definitions.Count);
    }

    public BugExperimentSession Session { get; }

    public IReadOnlyList<BugDefinition> Definitions { get; }

    public BugScenario EnabledScenarios => (BugScenario)Volatile.Read(ref _enabledScenarios);

    public BugTriggerMode TriggerMode => (BugTriggerMode)Volatile.Read(ref _triggerMode);

    public bool IsScenarioEnabled(BugScenario scenario)
    {
        ValidateSingleScenario(scenario);
        return EnabledScenarios.HasFlag(scenario);
    }

    public void SetScenarioEnabled(BugScenario scenario, bool enabled)
    {
        ValidateSingleScenario(scenario);

        while (true)
        {
            var current = Volatile.Read(ref _enabledScenarios);
            var updated = enabled
                ? current | (int)scenario
                : current & ~(int)scenario;

            if (Interlocked.CompareExchange(ref _enabledScenarios, updated, current) == current)
            {
                LogConfigurationChanged();
                return;
            }
        }
    }

    public void EnableAll()
    {
        Interlocked.Exchange(ref _enabledScenarios, (int)BugScenario.All);
        LogConfigurationChanged();
    }

    public void DisableAll()
    {
        Interlocked.Exchange(ref _enabledScenarios, (int)BugScenario.None);
        LogConfigurationChanged();
    }

    public void SetTriggerMode(BugTriggerMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        Interlocked.Exchange(ref _triggerMode, (int)mode);
        LogConfigurationChanged();
    }

    public bool IsEnabled(string bugId)
    {
        var definition = GetDefinition(bugId);
        return EnabledScenarios.HasFlag(definition.Scenario)
            && !_options.DisabledBugIds.Contains(definition.Id);
    }

    public BugDefinition GetDefinition(string bugId)
    {
        return _catalog.GetRequired(bugId);
    }

    public bool TryTrigger(string bugId, string? detail = null)
    {
        if (!IsEnabled(bugId))
        {
            return false;
        }

        RecordTriggered(bugId, detail);

        return TriggerMode switch
        {
            BugTriggerMode.Natural => true,
            BugTriggerMode.RecordOnly => false,
            BugTriggerMode.CrashImmediately => throw new InjectedBugException(
                bugId,
                detail ?? "Configured to crash immediately when the bug trigger is reached."),
            _ => throw new InvalidOperationException($"Unknown trigger mode '{TriggerMode}'.")
        };
    }

    public void RecordTriggered(string bugId, string? detail = null)
    {
        var definition = GetDefinition(bugId);
        var record = new BugTriggerRecord(
            Interlocked.Increment(ref _sequence),
            DateTimeOffset.UtcNow,
            Session.SessionId,
            definition.Id,
            definition.Scenario,
            definition.Page,
            definition.Operation,
            detail);

        _triggerRecords.Enqueue(record);

        while (_triggerRecords.Count > MaximumTriggerRecords)
        {
            _triggerRecords.TryDequeue(out _);
        }

        _logger.LogError(
            BugInjectionLogEvents.BugTriggered,
            "Injected bug triggered. SessionId={SessionId}, BugId={BugId}, Scenario={Scenario}, Page={Page}, Operation={Operation}, Detail={Detail}",
            record.SessionId,
            record.BugId,
            record.Scenario,
            record.Page,
            record.Operation,
            record.Detail);
    }

    public IReadOnlyList<BugTriggerRecord> GetTriggeredSnapshot()
    {
        return _triggerRecords.OrderByDescending(record => record.Sequence).ToArray();
    }

    public void ClearTriggeredRecords()
    {
        _triggerRecords.Clear();
    }

    private void LogConfigurationChanged()
    {
        _logger.LogInformation(
            BugInjectionLogEvents.ConfigurationChanged,
            "Bug injection configuration changed. SessionId={SessionId}, EnabledScenarios={EnabledScenarios}, TriggerMode={TriggerMode}",
            Session.SessionId,
            EnabledScenarios,
            TriggerMode);
    }

    private static void ValidateSingleScenario(BugScenario scenario)
    {
        if (scenario == BugScenario.None
            || scenario == BugScenario.All
            || !Enum.IsDefined(scenario))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scenario),
                "A single bug scenario must be specified.");
        }
    }
}
