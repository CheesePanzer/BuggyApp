using BuggyApp.BugInjection;

namespace BuggyApp.Services;

public sealed class DialogService(IBugInjectionManager bugManager) : IDialogService
{
    private readonly SemaphoreSlim _dialogLock = new(1, 1);
    private int _dialogInProgress;

    public async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = Shell.Current?.CurrentPage
            ?? throw new InvalidOperationException("No active page is available for a dialog.");

        if (bugManager.IsEnabled(BugIds.ConfirmationDialogReentry))
        {
            if (!TryEnterBuggyDialog(title))
            {
                return false;
            }

            try
            {
                return await page.DisplayAlertAsync(title, message, accept, cancel);
            }
            finally
            {
                Interlocked.Exchange(ref _dialogInProgress, 0);
            }
        }

        await _dialogLock.WaitAsync();
        try
        {
            return await page.DisplayAlertAsync(title, message, accept, cancel);
        }
        finally
        {
            _dialogLock.Release();
        }
    }

    public async Task AlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Shell.Current?.CurrentPage
            ?? throw new InvalidOperationException("No active page is available for a dialog.");

        if (bugManager.IsEnabled(BugIds.ConfirmationDialogReentry))
        {
            if (!TryEnterBuggyDialog(title))
            {
                return;
            }

            try
            {
                await page.DisplayAlertAsync(title, message, cancel);
                return;
            }
            finally
            {
                Interlocked.Exchange(ref _dialogInProgress, 0);
            }
        }

        await _dialogLock.WaitAsync();
        try
        {
            await page.DisplayAlertAsync(title, message, cancel);
        }
        finally
        {
            _dialogLock.Release();
        }
    }

    private bool TryEnterBuggyDialog(string title)
    {
        if (Interlocked.CompareExchange(ref _dialogInProgress, 1, 0) == 0)
        {
            return true;
        }

        if (bugManager.TryTrigger(
            BugIds.ConfirmationDialogReentry,
            $"DialogTitle={title}"))
        {
            throw new InjectedBugException(
                BugIds.ConfirmationDialogReentry,
                $"Dialog '{title}' was requested while another dialog was active.");
        }

        return false;
    }
}
