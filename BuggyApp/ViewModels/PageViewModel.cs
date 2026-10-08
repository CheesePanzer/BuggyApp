using CommunityToolkit.Mvvm.ComponentModel;

namespace BuggyApp.ViewModels;

public abstract partial class PageViewModel : ObservableObject
{
    private CancellationTokenSource? _pageCancellation;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public async Task AppearingAsync()
    {
        _pageCancellation?.Cancel();
        _pageCancellation?.Dispose();
        _pageCancellation = new CancellationTokenSource();

        try
        {
            await OnAppearingAsync(_pageCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Leaving a page is an expected cancellation path.
        }
    }

    public void Disappearing()
    {
        _pageCancellation?.Cancel();
        OnDisappearing();
    }

    protected CancellationToken PageCancellationToken =>
        _pageCancellation?.Token ?? CancellationToken.None;

    protected abstract Task OnAppearingAsync(CancellationToken cancellationToken);

    protected virtual void OnDisappearing()
    {
    }

    protected async Task RunBusyAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await action(PageCancellationToken);
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
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
