using System.Diagnostics;
using BuggyApp.Configuration;
using BuggyApp.Data;
using BuggyApp.Models;

namespace BuggyApp.Services;

public sealed class MockedCourseController : IMockedCourseController
{
    private readonly MockedDb _db;
    private readonly IMockDelayProvider _delayProvider;
    private readonly DiagnosticsService _diagnostics;
    private readonly MockServerOptions _options;

    public MockedCourseController(
        MockedDb db,
        IMockDelayProvider delayProvider,
        DiagnosticsService diagnostics,
        MockServerOptions options)
    {
        _db = db;
        _delayProvider = delayProvider;
        _diagnostics = diagnostics;
        _options = options;
    }

    public Task<DashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(GetDashboardAsync), _db.GetDashboardSummary, cancellationToken);

    public Task<PagedResult<Course>> GetCoursesAsync(
        string? category,
        int page,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            nameof(GetCoursesAsync),
            () => _db.GetCourses(category, page, _options.CoursePageSize),
            cancellationToken);

    public Task<Course?> GetCourseAsync(
        int courseId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(GetCourseAsync), () => _db.GetCourse(courseId), cancellationToken);

    public Task<bool> IsInBasketAsync(
        int courseId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(IsInBasketAsync), () => _db.IsInBasket(courseId), cancellationToken);

    public Task<bool> AddToBasketAsync(
        int courseId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(AddToBasketAsync), () => _db.AddToBasket(courseId), cancellationToken);

    public Task<bool> RemoveFromBasketAsync(
        int courseId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(RemoveFromBasketAsync), () => _db.RemoveFromBasket(courseId), cancellationToken);

    public Task<IReadOnlyList<Course>> GetBasketAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(GetBasketAsync), _db.GetBasket, cancellationToken);

    public Task ClearBasketAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(ClearBasketAsync), _db.ClearBasket, cancellationToken);

    public Task<Registration> RegisterAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(RegisterAsync), _db.RegisterBasket, cancellationToken);

    public Task<PagedResult<Registration>> GetRegistrationsAsync(
        int page,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            nameof(GetRegistrationsAsync),
            () => _db.GetRegistrations(page, _options.RegistrationPageSize),
            cancellationToken);

    public Task<Registration?> GetRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            nameof(GetRegistrationAsync),
            () => _db.GetRegistration(registrationId),
            cancellationToken);

    public Task<bool> DeleteRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            nameof(DeleteRegistrationAsync),
            () => _db.DeleteRegistration(registrationId),
            cancellationToken);

    public Task ClearHistoryAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(ClearHistoryAsync), _db.ClearHistory, cancellationToken);

    public Task ResetDemoAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(nameof(ResetDemoAsync), _db.Reset, cancellationToken);

    private async Task<T> ExecuteAsync<T>(
        string operation,
        Func<T> action,
        CancellationToken cancellationToken)
    {
        _diagnostics.RequestStarted(operation);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _delayProvider.DelayAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var result = action();
            _diagnostics.RequestCompleted(operation, stopwatch.Elapsed);
            return result;
        }
        catch (OperationCanceledException)
        {
            _diagnostics.RequestCancelled(operation, stopwatch.Elapsed);
            throw;
        }
        catch (Exception exception)
        {
            _diagnostics.RequestFailed(operation, stopwatch.Elapsed, exception);
            throw;
        }
    }

    private async Task ExecuteAsync(
        string operation,
        Action action,
        CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            operation,
            () =>
            {
                action();
                return true;
            },
            cancellationToken);
    }
}
