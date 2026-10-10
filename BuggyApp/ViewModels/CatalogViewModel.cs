using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using BuggyApp.BugInjection;
using BuggyApp.Models;
using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuggyApp.ViewModels;

public partial class CatalogViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private readonly IBugInjectionManager _bugManager;
    private readonly ConcurrentDictionary<int, byte> _activeBasketOperations = new();
    private readonly Dictionary<int, CourseItemViewModel> _courseIndex = [];
    private int _currentPage;
    private bool _hasMore;
    private bool _isLoadingMore;

    public CatalogViewModel(
        IMockedCourseController controller,
        INavigationService navigation,
        IBugInjectionManager bugManager)
    {
        _controller = controller;
        _navigation = navigation;
        _bugManager = bugManager;
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

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task LoadMoreAsync()
    {
        if (_bugManager.IsEnabled(BugIds.CatalogPagingRefreshRace))
        {
            await LoadMoreWithCollectionRaceAsync();
            return;
        }

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

    private async Task LoadMoreWithCollectionRaceAsync()
    {
        if (!_hasMore)
        {
            return;
        }

        var nextPage = _currentPage + 1;
        var category = SelectedCategory == "All" ? null : SelectedCategory;
        var coursesTask = _controller.GetCoursesAsync(
            category,
            nextPage,
            PageCancellationToken);
        var basketTask = _controller.GetBasketAsync(PageCancellationToken);

        await Task.WhenAll(coursesTask, basketTask);
        AppendCoursesWithRaceDetection(coursesTask.Result, basketTask.Result, nextPage, category);
        _currentPage = nextPage;
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ToggleBasketAsync(CourseItemViewModel course)
    {
        if (!_bugManager.IsEnabled(BugIds.CatalogToggleReentry))
        {
            await ToggleBasketProtectedAsync(course);
            return;
        }

        if (!_activeBasketOperations.TryAdd(course.Id, 0))
        {
            if (_bugManager.TryTrigger(
                BugIds.CatalogToggleReentry,
                $"CourseId={course.Id}"))
            {
                throw new InjectedBugException(
                    BugIds.CatalogToggleReentry,
                    $"Course {course.Id} received concurrent Add/Remove commands.");
            }

            return;
        }

        try
        {
            await ToggleBasketCoreAsync(course);
        }
        finally
        {
            _activeBasketOperations.TryRemove(course.Id, out _);
        }
    }

    private async Task ToggleBasketProtectedAsync(CourseItemViewModel course)
    {
        if (course.IsUpdating)
        {
            return;
        }

        course.IsUpdating = true;
        ErrorMessage = null;

        try
        {
            await ToggleBasketCoreAsync(course);
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

    private async Task ToggleBasketCoreAsync(CourseItemViewModel course)
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

    [RelayCommand(AllowConcurrentExecutions = true)]
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
        _courseIndex.Clear();
        AppendCourses(result, basket);
    }

    private void AppendCourses(
        PagedResult<Course> result,
        IReadOnlyList<Course> basket)
    {
        var basketIds = basket.Select(course => course.Id).ToHashSet();
        foreach (var course in result.Items)
        {
            var item = new CourseItemViewModel(course, basketIds.Contains(course.Id));
            Courses.Add(item);
            _courseIndex[course.Id] = item;
        }

        _hasMore = result.HasMore;
    }

    private void AppendCoursesWithRaceDetection(
        PagedResult<Course> result,
        IReadOnlyList<Course> basket,
        int requestedPage,
        string? requestedCategory)
    {
        var basketIds = basket.Select(course => course.Id).ToHashSet();
        foreach (var course in result.Items)
        {
            var item = new CourseItemViewModel(course, basketIds.Contains(course.Id));
            if (_courseIndex.ContainsKey(course.Id))
            {
                if (!_bugManager.TryTrigger(
                    BugIds.CatalogPagingRefreshRace,
                    $"CourseId={course.Id}, Page={requestedPage}, Category={requestedCategory ?? "All"}"))
                {
                    continue;
                }
            }

            // Dictionary.Add intentionally exposes a duplicate-key race in Natural mode.
            _courseIndex.Add(course.Id, item);
            Courses.Add(item);
        }

        _hasMore = result.HasMore;
    }
}
