using BuggyApp.BugInjection;
using BuggyApp.Models;
using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuggyApp.ViewModels;

public partial class CourseDetailViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IBugInjectionManager _bugManager;
    private int _courseId;
    private bool _isPageActive;

    public CourseDetailViewModel(
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionText))]
    public partial Course? Course { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionText))]
    public partial bool IsInBasket { get; set; }

    public string ActionText => IsInBasket ? "Remove from basket" : "Add to basket";

    public void ApplyCourseId(int courseId)
    {
        _courseId = courseId;
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
        if (_courseId <= 0)
        {
            ErrorMessage = "No course was selected.";
            return;
        }

        var requestToken = _bugManager.IsEnabled(BugIds.CourseDetailAfterClose)
            ? CancellationToken.None
            : cancellationToken;
        var courseTask = _controller.GetCourseAsync(_courseId, requestToken);
        var basketTask = _controller.IsInBasketAsync(_courseId, requestToken);
        await Task.WhenAll(courseTask, basketTask);

        if (!_isPageActive
            && _bugManager.TryTrigger(
                BugIds.CourseDetailAfterClose,
                $"CourseId={_courseId}"))
        {
            throw new InjectedBugException(
                BugIds.CourseDetailAfterClose,
                $"Course {_courseId} completed loading after its page closed.");
        }

        Course = courseTask.Result;
        IsInBasket = basketTask.Result;

        if (Course is null)
        {
            ErrorMessage = "The selected course no longer exists.";
        }
    });

    [RelayCommand]
    private async Task ToggleBasketAsync()
    {
        if (Course is null || IsBusy)
        {
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            if (IsInBasket)
            {
                await _controller.RemoveFromBasketAsync(Course.Id, cancellationToken);
                IsInBasket = false;
            }
            else
            {
                await _controller.AddToBasketAsync(Course.Id, cancellationToken);
                IsInBasket = true;
            }
        });
    }

    [RelayCommand]
    private Task ShowInformationAsync()
    {
        if (Course is null)
        {
            return Task.CompletedTask;
        }

        return _dialogs.AlertAsync(Course.Code, Course.Description);
    }

    [RelayCommand]
    private Task GoBackAsync() => _navigation.GoBackAsync(PageCancellationToken);
}
