namespace BuggyApp.BugInjection;

public sealed record BugDefinition(
    string Id,
    BugScenario Scenario,
    string Page,
    string Operation,
    string Description);
