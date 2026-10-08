using System.Collections.ObjectModel;
using BuggyApp.Models;
using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuggyApp.ViewModels;

public partial class RegistrationDetailViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private Guid _registrationId;

    public RegistrationDetailViewModel(
        IMockedCourseController controller,
        INavigationService navigation,
        IDialogService dialogs)
    {
        _controller = controller;
        _navigation = navigation;
        _dialogs = dialogs;
    }

    public ObservableCollection<CourseItemViewModel> Courses { get; } = [];

    [ObservableProperty]
    public partial string RegistrationCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CreatedAtText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int CourseCount { get; set; }

    [ObservableProperty]
    public partial bool RecordExists { get; set; }

    public void ApplyRegistrationId(Guid registrationId)
    {
        _registrationId = registrationId;
    }

    protected override Task OnAppearingAsync(CancellationToken cancellationToken) => LoadAsync();

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async cancellationToken =>
    {
        if (_registrationId == Guid.Empty)
        {
            ErrorMessage = "No registration was selected.";
            RecordExists = false;
            return;
        }

        var registration = await _controller.GetRegistrationAsync(_registrationId, cancellationToken);
        if (registration is null)
        {
            ErrorMessage = "This registration record no longer exists.";
            RecordExists = false;
            Courses.Clear();
            return;
        }

        RecordExists = true;
        RegistrationCode = registration.Id.ToString("N")[..8].ToUpperInvariant();
        CreatedAtText = registration.CreatedAt.LocalDateTime.ToString("MMM d, yyyy · h:mm tt");
        CourseCount = registration.Courses.Count;
        ReplaceCourses(registration.Courses);
    });

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!RecordExists || IsBusy)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Delete registration?",
            $"Delete registration {RegistrationCode}?",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            await _controller.DeleteRegistrationAsync(_registrationId, cancellationToken);
            RecordExists = false;
            Courses.Clear();
            await _navigation.GoBackAsync();
        });
    }

    [RelayCommand]
    private Task OpenCourseAsync(CourseItemViewModel course) =>
        _navigation.GoToAsync(
            "course-detail",
            new Dictionary<string, object> { ["CourseId"] = course.Id },
            PageCancellationToken);

    [RelayCommand]
    private Task GoBackAsync() => _navigation.GoBackAsync(PageCancellationToken);

    private void ReplaceCourses(IEnumerable<Course> courses)
    {
        Courses.Clear();
        foreach (var course in courses)
        {
            Courses.Add(new CourseItemViewModel(course, false));
        }
    }
}
