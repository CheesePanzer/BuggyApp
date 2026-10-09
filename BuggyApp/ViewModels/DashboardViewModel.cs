using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuggyApp.ViewModels;

public partial class DashboardViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private int _titleTapCount;

    public DashboardViewModel(
        IMockedCourseController controller,
        INavigationService navigation)
    {
        _controller = controller;
        _navigation = navigation;
    }

    [ObservableProperty]
    public partial int CourseCount { get; set; }

    [ObservableProperty]
    public partial int BasketCount { get; set; }

    [ObservableProperty]
    public partial int RegistrationCount { get; set; }

    protected override Task OnAppearingAsync(CancellationToken cancellationToken) =>
        LoadAsync();

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async cancellationToken =>
    {
        var summary = await _controller.GetDashboardAsync(cancellationToken);
        CourseCount = summary.CourseCount;
        BasketCount = summary.BasketCount;
        RegistrationCount = summary.RegistrationCount;
    });

    [RelayCommand]
    private Task OpenCatalogAsync() => _navigation.GoToAsync("//catalog");

    [RelayCommand]
    private Task OpenBasketAsync() => _navigation.GoToAsync("//basket");

    [RelayCommand]
    private Task OpenHistoryAsync() => _navigation.GoToAsync("//history");

    [RelayCommand]
    private async Task UnlockMonkeyTestAsync()
    {
        _titleTapCount++;
        if (_titleTapCount < 15)
        {
            return;
        }

        _titleTapCount = 0;
        await _navigation.GoToAsync("monkey-test", cancellationToken: PageCancellationToken);
    }
}
