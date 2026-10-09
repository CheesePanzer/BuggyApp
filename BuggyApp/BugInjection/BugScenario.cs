namespace BuggyApp.BugInjection;

[Flags]
public enum BugScenario
{
    None = 0,
    CommandReentry = 1 << 0,
    CollectionRace = 1 << 1,
    Lifecycle = 1 << 2,
    NavigationReentry = 1 << 3,
    UiThreadBlocking = 1 << 4,
    All = CommandReentry
        | CollectionRace
        | Lifecycle
        | NavigationReentry
        | UiThreadBlocking
}
