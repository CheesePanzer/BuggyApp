using BuggyApp.BugInjection;

namespace BuggyApp.Services;

public sealed class ShellNavigationService : INavigationService
{
    private readonly DiagnosticsService _diagnostics;
    private readonly IBugInjectionManager _bugManager;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private int _navigationInProgress;

    public ShellNavigationService(
        DiagnosticsService diagnostics,
        IBugInjectionManager bugManager)
    {
        _diagnostics = diagnostics;
        _bugManager = bugManager;
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

        var bugId = GetNavigationBugId(route);
        if (bugId is not null && _bugManager.IsEnabled(bugId))
        {
            await NavigateWithReentryBugAsync(
                shell,
                route,
                parameters,
                bugId,
                cancellationToken);
            return;
        }

        await _navigationLock.WaitAsync(cancellationToken);
        try
        {
            await NavigateCoreAsync(shell, route, parameters);
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    private async Task NavigateWithReentryBugAsync(
        Shell shell,
        string route,
        IReadOnlyDictionary<string, object>? parameters,
        string bugId,
        CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _navigationInProgress, 1, 0) != 0)
        {
            if (_bugManager.TryTrigger(bugId, $"Route={route}"))
            {
                throw new InjectedBugException(
                    bugId,
                    $"A second navigation to '{route}' started while another navigation was active.");
            }

            return;
        }

        try
        {
            // Keep the transition open long enough for random repeated taps.
            await Task.Delay(500, cancellationToken);
            await NavigateCoreAsync(shell, route, parameters);
        }
        finally
        {
            Interlocked.Exchange(ref _navigationInProgress, 0);
        }
    }

    private static Task NavigateCoreAsync(
        Shell shell,
        string route,
        IReadOnlyDictionary<string, object>? parameters)
    {
        if (parameters is null)
        {
            return shell.GoToAsync(route);
        }

        return shell.GoToAsync(route, new Dictionary<string, object>(parameters));
    }

    private static string? GetNavigationBugId(string route)
    {
        return route switch
        {
            "course-detail" => BugIds.CatalogDetailsNavigationReentry,
            "registration-detail" => BugIds.HistoryDetailsNavigationReentry,
            _ => null
        };
    }

    public Task GoBackAsync(CancellationToken cancellationToken = default)
    {
        return GoToAsync("..", cancellationToken: cancellationToken);
    }
}
