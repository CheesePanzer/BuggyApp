namespace BuggyApp.Services;

public sealed class ShellNavigationService : INavigationService
{
    private readonly DiagnosticsService _diagnostics;

    public ShellNavigationService(DiagnosticsService diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public async Task GoToAsync(
        string route,
        IReadOnlyDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        cancellationToken.ThrowIfCancellationRequested();

        var shell = Shell.Current
            ?? throw new InvalidOperationException("Shell navigation is not available.");

        _diagnostics.RecordNavigation(route);

        if (parameters is null)
        {
            await shell.GoToAsync(route);
        }
        else
        {
            await shell.GoToAsync(route, new Dictionary<string, object>(parameters));
        }
    }

    public Task GoBackAsync(CancellationToken cancellationToken = default)
    {
        return GoToAsync("..", cancellationToken: cancellationToken);
    }
}
