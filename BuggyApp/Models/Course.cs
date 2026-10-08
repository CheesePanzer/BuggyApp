namespace BuggyApp.Models;

public sealed record Course(
    int Id,
    string Code,
    string Title,
    string Category,
    string Instructor,
    string Description);
