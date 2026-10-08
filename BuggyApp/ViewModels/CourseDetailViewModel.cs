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
    private int _courseId;

    public CourseDetailViewModel(
        IMockedCourseController controller,
        INavigationService navigation,
        IDialogService dialogs)
    {
        _controller = controller;
        _navigation = navigation;
        _dialogs = dialogs;
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

    protected override Task OnAppearingAsync(CancellationToken cancellationToken) =>
        LoadAsync();

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async cancellationToken =>
    {
        if (_courseId <= 0)
        {
            ErrorMessage = "No course was selected.";
            return;
        }

        var courseTask = _controller.GetCourseAsync(_courseId, cancellationToken);
        var basketTask = _controller.IsInBasketAsync(_courseId, cancellationToken);
        await Task.WhenAll(courseTask, basketTask);

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
