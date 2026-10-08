namespace BuggyApp.Services;

public interface INavigationService
{
    Task GoToAsync(
        string route,
        IReadOnlyDictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default);

    Task GoBackAsync(CancellationToken cancellationToken = default);
}
