namespace BuggyApp.Services;

public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        var page = Shell.Current?.CurrentPage
            ?? throw new InvalidOperationException("No active page is available for a dialog.");

        return page.DisplayAlertAsync(title, message, accept, cancel);
    }

    public Task AlertAsync(string title, string message, string cancel = "OK")
    {
        var page = Shell.Current?.CurrentPage
            ?? throw new InvalidOperationException("No active page is available for a dialog.");

        return page.DisplayAlertAsync(title, message, cancel);
    }
}
