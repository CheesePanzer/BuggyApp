namespace BuggyApp.Models;

public sealed record Registration(
    Guid Id,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Course> Courses);
