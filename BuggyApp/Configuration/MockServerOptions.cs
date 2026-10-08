namespace BuggyApp.Configuration;

public sealed class MockServerOptions
{
    public int RandomSeed { get; init; } = 12345;

    public int MinimumDelayMilliseconds { get; init; } = 100;

    public int MaximumDelayMilliseconds { get; init; } = 2500;

    public int CoursePageSize { get; init; } = 30;

    public int RegistrationPageSize { get; init; } = 20;
}
