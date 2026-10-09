using Microsoft.Extensions.Logging;

namespace BuggyApp.BugInjection;

public static class BugInjectionLogEvents
{
    public static readonly EventId SessionStarted = new(9000, nameof(SessionStarted));

    public static readonly EventId BugTriggered = new(9001, nameof(BugTriggered));

    public static readonly EventId ConfigurationChanged = new(9002, nameof(ConfigurationChanged));
}
