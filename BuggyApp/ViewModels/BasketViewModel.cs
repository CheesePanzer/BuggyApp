using System.Collections.ObjectModel;
using BuggyApp.BugInjection;
using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuggyApp.ViewModels;

public partial class BasketViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IBugInjectionManager _bugManager;
    private int _registrationInProgress;
    private bool _isPageActive;

    public BasketViewModel(
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
    [NotifyPropertyChangedFor(nameof(HasCourses))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial int CourseCount { get; set; }

    public bool HasCourses => CourseCount > 0;

    public bool IsEmpty => !HasCourses;

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
        IsRefreshing = true;
        var courses = await _controller.GetBasketAsync(cancellationToken);
        ReplaceCourses(courses.Select(course => new CourseItemViewModel(course, true)));
    });

    [RelayCommand]
    private async Task RemoveAsync(CourseItemViewModel course)
    {
        if (_bugManager.IsEnabled(BugIds.BasketRemoveClearRace))
        {
            await RemoveWithCollectionRaceAsync(course);
            return;
        }

        if (course.IsUpdating)
        {
            return;
        }

        course.IsUpdating = true;
        ErrorMessage = null;

        try
        {
            await _controller.RemoveFromBasketAsync(course.Id, PageCancellationToken);
            Courses.Remove(course);
            CourseCount = Courses.Count;
        }
        catch (OperationCanceledException) when (PageCancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            course.IsUpdating = false;
        }
    }

    private async Task RemoveWithCollectionRaceAsync(CourseItemViewModel course)
    {
        var staleIndex = Courses.IndexOf(course);
        if (staleIndex < 0)
        {
            return;
        }

        await _controller.RemoveFromBasketAsync(course.Id, PageCancellationToken);

        if (staleIndex >= Courses.Count || Courses[staleIndex] != course)
        {
            if (!_bugManager.TryTrigger(
                BugIds.BasketRemoveClearRace,
                $"CourseId={course.Id}, StaleIndex={staleIndex}, CurrentCount={Courses.Count}"))
            {
                return;
            }
        }

        // The stale index is intentionally used in Natural mode.
        Courses.RemoveAt(staleIndex);
        CourseCount = Courses.Count;
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ClearAsync()
    {
        if (!HasCourses || (IsBusy && !_bugManager.IsEnabled(BugIds.BasketRemoveClearRace)))
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Clear basket?",
            "Remove every selected course from the basket?",
            "Clear",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        if (_bugManager.IsEnabled(BugIds.BasketRemoveClearRace))
        {
            await _controller.ClearBasketAsync(PageCancellationToken);
            Courses.Clear();
            CourseCount = 0;
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            await _controller.ClearBasketAsync(cancellationToken);
            Courses.Clear();
            CourseCount = 0;
        });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task RegisterAsync()
    {
        if (_bugManager.IsEnabled(BugIds.BasketRegisterAfterClose))
        {
            await RegisterWithLifecycleBugAsync();
            return;
        }

        if (_bugManager.IsEnabled(BugIds.BasketRegisterReentry))
        {
            await RegisterWithReentryBugAsync();
            return;
        }

        if (!HasCourses || IsBusy)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Confirm registration?",
            $"Register the {CourseCount} selected courses?",
            "Register",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            var registration = await _controller.RegisterAsync(cancellationToken);
            Courses.Clear();
            CourseCount = 0;
            await _navigation.GoToAsync(
                "registration-result",
                new Dictionary<string, object> { ["RegistrationId"] = registration.Id });
        });
    }

    private async Task RegisterWithLifecycleBugAsync()
    {
        if (!HasCourses || IsBusy)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Confirm registration?",
            $"Register the {CourseCount} selected courses?",
            "Register",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var registration = await _controller.RegisterAsync(CancellationToken.None);

            if (!_isPageActive
                && _bugManager.TryTrigger(
                    BugIds.BasketRegisterAfterClose,
                    $"RegistrationId={registration.Id}"))
            {
                throw new InjectedBugException(
                    BugIds.BasketRegisterAfterClose,
                    "Registration completed after the Basket page closed.");
            }

            Courses.Clear();
            CourseCount = 0;
            await _navigation.GoToAsync(
                "registration-result",
                new Dictionary<string, object> { ["RegistrationId"] = registration.Id });
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RegisterWithReentryBugAsync()
    {
        if (!HasCourses)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _registrationInProgress, 1, 0) != 0)
        {
            if (_bugManager.TryTrigger(
                BugIds.BasketRegisterReentry,
                $"BasketCourseCount={CourseCount}"))
            {
                throw new InjectedBugException(
                    BugIds.BasketRegisterReentry,
                    "The registration command was entered while another submission was active.");
            }

            return;
        }

        try
        {
            var confirmed = await _dialogs.ConfirmAsync(
                "Confirm registration?",
                $"Register the {CourseCount} selected courses?",
                "Register",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            var registration = await _controller.RegisterAsync(PageCancellationToken);
            Courses.Clear();
            CourseCount = 0;
            await _navigation.GoToAsync(
                "registration-result",
                new Dictionary<string, object> { ["RegistrationId"] = registration.Id });
        }
        finally
        {
            Interlocked.Exchange(ref _registrationInProgress, 0);
        }
    }

    [RelayCommand]
    private Task OpenCatalogAsync() => _navigation.GoToAsync("//catalog");

    [RelayCommand]
    private Task OpenDetailsAsync(CourseItemViewModel course) =>
        _navigation.GoToAsync(
            "course-detail",
            new Dictionary<string, object> { ["CourseId"] = course.Id },
            PageCancellationToken);

    private void ReplaceCourses(IEnumerable<CourseItemViewModel> courses)
    {
        Courses.Clear();
        foreach (var course in courses)
        {
            Courses.Add(course);
        }

        CourseCount = Courses.Count;
    }
}
