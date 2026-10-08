using BuggyApp.Models;

namespace BuggyApp.ViewModels;

public sealed class RegistrationItemViewModel
{
    public RegistrationItemViewModel(Registration registration)
    {
        Registration = registration;
    }

    public Registration Registration { get; }

    public Guid Id => Registration.Id;

    public string ShortId => Registration.Id.ToString("N")[..8].ToUpperInvariant();

    public string CreatedAtText => Registration.CreatedAt.LocalDateTime.ToString("MMM d, yyyy · h:mm tt");

    public int CourseCount => Registration.Courses.Count;

    public string CourseCountText => CourseCount == 1 ? "1 course" : $"{CourseCount} courses";

    public string CourseCodes => string.Join(" · ", Registration.Courses.Take(4).Select(course => course.Code));
}
