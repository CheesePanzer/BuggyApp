using System.Collections.ObjectModel;
using BuggyApp.BugInjection;
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
    private readonly IBugInjectionManager _bugManager;
    private Guid _registrationId;
    private bool _isPageActive;

    public RegistrationDetailViewModel(
        IMockedCourseController controller,
        INavigationService navigation,
        IDialogService dialogs,
        IBugInjectionManager bugManager)
    {
        _controller = controller;
        _navigation = navigation;
        _dialogs = dialogs;
        _bugManager = bugManager;
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

    protected override Task OnAppearingAsync(CancellationToken cancellationToken)
    {
        _isPageActive = true;
        return LoadAsync();
    }

    protected override void OnDisappearing()
    {
        _isPageActive = false;
    }

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async cancellationToken =>
    {
        if (_registrationId == Guid.Empty)
        {
            ErrorMessage = "No registration was selected.";
            RecordExists = false;
            return;
        }

        var requestToken = _bugManager.IsEnabled(BugIds.RegistrationDetailAfterClose)
            ? CancellationToken.None
            : cancellationToken;
        var registration = await _controller.GetRegistrationAsync(_registrationId, requestToken);

        if (!_isPageActive
            && _bugManager.TryTrigger(
                BugIds.RegistrationDetailAfterClose,
                $"RegistrationId={_registrationId}"))
        {
            throw new InjectedBugException(
                BugIds.RegistrationDetailAfterClose,
                $"Registration {_registrationId} completed loading after its page closed.");
        }

        if (registration is null)
        {
            if (_bugManager.TryTrigger(
                BugIds.HistoryDeleteOpenRace,
                $"RegistrationId={_registrationId}"))
            {
                throw new InjectedBugException(
                    BugIds.HistoryDeleteOpenRace,
                    $"Registration {_registrationId} was deleted before its detail request completed.");
            }

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
