namespace BuggyApp.Services;

public interface IMockDelayProvider
{
    Task DelayAsync(CancellationToken cancellationToken = default);
}
