using System.Collections.ObjectModel;
using BuggyApp.Models;
using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuggyApp.ViewModels;

public partial class CatalogViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private int _currentPage;
    private bool _hasMore;
    private bool _isLoadingMore;

    public CatalogViewModel(
        IMockedCourseController controller,
        INavigationService navigation)
    {
        _controller = controller;
        _navigation = navigation;
    }

    public ObservableCollection<CourseItemViewModel> Courses { get; } = [];

    public IReadOnlyList<string> Categories { get; } =
    [
        "All",
        "Computing",
        "Business",
        "Engineering",
        "Science",
        "Arts",
        "Humanities"
    ];

    [ObservableProperty]
    public partial string SelectedCategory { get; set; } = "All";

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    protected override Task OnAppearingAsync(CancellationToken cancellationToken) =>
        RefreshAsync();

    partial void OnSelectedCategoryChanged(string value)
    {
        if (!IsBusy)
        {
            _ = RefreshAsync();
        }
    }

    [RelayCommand]
    private Task SelectCategoryAsync(string category)
    {
        if (SelectedCategory == category)
        {
            return Task.CompletedTask;
        }

        SelectedCategory = category;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(async cancellationToken =>
    {
        IsRefreshing = true;
        _currentPage = 1;

        var coursesTask = _controller.GetCoursesAsync(
            SelectedCategory == "All" ? null : SelectedCategory,
            _currentPage,
            cancellationToken);
        var basketTask = _controller.GetBasketAsync(cancellationToken);

        await Task.WhenAll(coursesTask, basketTask);
        ReplaceCourses(coursesTask.Result, basketTask.Result);
    });

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || _isLoadingMore || !_hasMore)
        {
            return;
        }

        _isLoadingMore = true;
        IsLoadingMore = true;
        ErrorMessage = null;

        try
        {
            var nextPage = _currentPage + 1;
            var coursesTask = _controller.GetCoursesAsync(
                SelectedCategory == "All" ? null : SelectedCategory,
                nextPage,
                PageCancellationToken);
            var basketTask = _controller.GetBasketAsync(PageCancellationToken);

            await Task.WhenAll(coursesTask, basketTask);
            AppendCourses(coursesTask.Result, basketTask.Result);
            _currentPage = nextPage;
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
            _isLoadingMore = false;
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task ToggleBasketAsync(CourseItemViewModel course)
    {
        if (course.IsUpdating)
        {
            return;
        }

        course.IsUpdating = true;
        ErrorMessage = null;

        try
        {
            if (course.IsInBasket)
            {
                await _controller.RemoveFromBasketAsync(course.Id, PageCancellationToken);
                course.IsInBasket = false;
            }
            else
            {
                await _controller.AddToBasketAsync(course.Id, PageCancellationToken);
                course.IsInBasket = true;
            }
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
    private Task OpenDetailsAsync(CourseItemViewModel course) =>
        _navigation.GoToAsync(
            "course-detail",
            new Dictionary<string, object> { ["CourseId"] = course.Id },
            PageCancellationToken);

    private void ReplaceCourses(
        PagedResult<Course> result,
        IReadOnlyList<Course> basket)
    {
        Courses.Clear();
        AppendCourses(result, basket);
    }

    private void AppendCourses(
        PagedResult<Course> result,
        IReadOnlyList<Course> basket)
    {
        var basketIds = basket.Select(course => course.Id).ToHashSet();
        foreach (var course in result.Items)
        {
            Courses.Add(new CourseItemViewModel(course, basketIds.Contains(course.Id)));
        }

        _hasMore = result.HasMore;
    }
}
