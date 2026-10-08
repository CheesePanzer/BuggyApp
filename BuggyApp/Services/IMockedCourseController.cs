using BuggyApp.Models;

namespace BuggyApp.Services;

public interface IMockedCourseController
{
    Task<DashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<Course>> GetCoursesAsync(
        string? category,
        int page,
        CancellationToken cancellationToken = default);

    Task<Course?> GetCourseAsync(int courseId, CancellationToken cancellationToken = default);

    Task<bool> IsInBasketAsync(int courseId, CancellationToken cancellationToken = default);

    Task<bool> AddToBasketAsync(int courseId, CancellationToken cancellationToken = default);

    Task<bool> RemoveFromBasketAsync(int courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Course>> GetBasketAsync(CancellationToken cancellationToken = default);

    Task ClearBasketAsync(CancellationToken cancellationToken = default);

    Task<Registration> RegisterAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<Registration>> GetRegistrationsAsync(
        int page,
        CancellationToken cancellationToken = default);

    Task<Registration?> GetRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default);

    Task ClearHistoryAsync(CancellationToken cancellationToken = default);

    Task ResetDemoAsync(CancellationToken cancellationToken = default);
}
