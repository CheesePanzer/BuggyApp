using System.Collections.ObjectModel;
using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuggyApp.ViewModels;

public partial class BasketViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    public BasketViewModel(
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
    [NotifyPropertyChangedFor(nameof(HasCourses))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial int CourseCount { get; set; }

    public bool HasCourses => CourseCount > 0;

    public bool IsEmpty => !HasCourses;

    protected override Task OnAppearingAsync(CancellationToken cancellationToken) =>
        LoadAsync();

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

    [RelayCommand]
    private async Task ClearAsync()
    {
        if (!HasCourses || IsBusy)
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

        await RunBusyAsync(async cancellationToken =>
        {
            await _controller.ClearBasketAsync(cancellationToken);
            Courses.Clear();
            CourseCount = 0;
        });
    }

    [RelayCommand]
    private async Task RegisterAsync()
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
