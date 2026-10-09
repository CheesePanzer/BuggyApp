using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using BuggyApp.BugInjection;
using BuggyApp.Models;
using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuggyApp.ViewModels;

public partial class HistoryViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IBugInjectionManager _bugManager;
    private readonly ConcurrentDictionary<Guid, byte> _activeDeleteOperations = new();
    private int _currentPage;
    private bool _hasMore;
    private bool _loadingMore;

    public HistoryViewModel(
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

    public ObservableCollection<RegistrationItemViewModel> Registrations { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRegistrations))]
    public partial int RegistrationCount { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    public bool HasRegistrations => RegistrationCount > 0;

    protected override Task OnAppearingAsync(CancellationToken cancellationToken) => RefreshAsync();

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(async cancellationToken =>
    {
        IsRefreshing = true;
        _currentPage = 1;
        var result = await _controller.GetRegistrationsAsync(_currentPage, cancellationToken);
        ReplaceRegistrations(result);
    });

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || _loadingMore || !_hasMore)
        {
            return;
        }

        _loadingMore = true;
        IsLoadingMore = true;
        ErrorMessage = null;

        try
        {
            var nextPage = _currentPage + 1;
            var result = await _controller.GetRegistrationsAsync(nextPage, PageCancellationToken);
            AppendRegistrations(result);
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
            _loadingMore = false;
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private Task OpenDetailsAsync(RegistrationItemViewModel registration) =>
        _navigation.GoToAsync(
            "registration-detail",
            new Dictionary<string, object> { ["RegistrationId"] = registration.Id },
            PageCancellationToken);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task DeleteAsync(RegistrationItemViewModel registration)
    {
        if (_bugManager.IsEnabled(BugIds.HistoryDeleteReentry))
        {
            await DeleteWithReentryBugAsync(registration);
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Delete registration?",
            $"Delete registration {registration.ShortId}?",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            await _controller.DeleteRegistrationAsync(registration.Id, cancellationToken);
            Registrations.Remove(registration);
            RegistrationCount = Registrations.Count;
        });
    }

    private async Task DeleteWithReentryBugAsync(RegistrationItemViewModel registration)
    {
        if (!_activeDeleteOperations.TryAdd(registration.Id, 0))
        {
            if (_bugManager.TryTrigger(
                BugIds.HistoryDeleteReentry,
                $"RegistrationId={registration.Id}"))
            {
                throw new InjectedBugException(
                    BugIds.HistoryDeleteReentry,
                    $"Registration {registration.Id} received concurrent delete commands.");
            }

            return;
        }

        try
        {
            var confirmed = await _dialogs.ConfirmAsync(
                "Delete registration?",
                $"Delete registration {registration.ShortId}?",
                "Delete",
                "Cancel");

            if (!confirmed)
            {
                return;
            }

            await _controller.DeleteRegistrationAsync(
                registration.Id,
                PageCancellationToken);
            Registrations.Remove(registration);
            RegistrationCount = Registrations.Count;
        }
        finally
        {
            _activeDeleteOperations.TryRemove(registration.Id, out _);
        }
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        if (!HasRegistrations || IsBusy)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Clear registration history?",
            "Every registration record will be removed.",
            "Clear",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            await _controller.ClearHistoryAsync(cancellationToken);
            Registrations.Clear();
            RegistrationCount = 0;
            _hasMore = false;
        });
    }

    private void ReplaceRegistrations(PagedResult<Registration> result)
    {
        Registrations.Clear();
        AppendRegistrations(result);
    }

    private void AppendRegistrations(PagedResult<Registration> result)
    {
        foreach (var registration in result.Items)
        {
            Registrations.Add(new RegistrationItemViewModel(registration));
        }

        RegistrationCount = result.TotalCount;
        _hasMore = result.HasMore;
    }
}
