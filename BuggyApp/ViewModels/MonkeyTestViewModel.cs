using BuggyApp.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BuggyApp.BugInjection;

namespace BuggyApp.ViewModels;

public partial class MonkeyTestViewModel : PageViewModel
{
    private readonly IMockedCourseController _controller;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly IBugInjectionManager _bugManager;
    private bool _synchronizingBugConfiguration;

    public MonkeyTestViewModel(
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
    public partial int CourseCount { get; set; }

    [ObservableProperty]
    public partial int BasketCount { get; set; }

    [ObservableProperty]
    public partial int RegistrationCount { get; set; }

    [ObservableProperty]
    public partial bool CommandReentryEnabled { get; set; }

    [ObservableProperty]
    public partial bool CollectionRaceEnabled { get; set; }

    [ObservableProperty]
    public partial bool LifecycleEnabled { get; set; }

    [ObservableProperty]
    public partial bool NavigationReentryEnabled { get; set; }

    [ObservableProperty]
    public partial bool UiThreadBlockingEnabled { get; set; }

    [ObservableProperty]
    public partial bool StateRaceEnabled { get; set; }

    [ObservableProperty]
    public partial int SelectedTriggerModeIndex { get; set; }

    [ObservableProperty]
    public partial string EnabledScenariosText { get; set; } = "None";

    public IReadOnlyList<string> TriggerModes { get; } =
    [
        "Natural",
        "Record only",
        "Crash immediately"
    ];

    protected override Task OnAppearingAsync(CancellationToken cancellationToken)
    {
        SynchronizeBugConfiguration();
        return LoadAsync();
    }

    partial void OnCommandReentryEnabledChanged(bool value) =>
        UpdateScenario(BugScenario.CommandReentry, value);

    partial void OnCollectionRaceEnabledChanged(bool value) =>
        UpdateScenario(BugScenario.CollectionRace, value);

    partial void OnLifecycleEnabledChanged(bool value) =>
        UpdateScenario(BugScenario.Lifecycle, value);

    partial void OnNavigationReentryEnabledChanged(bool value) =>
        UpdateScenario(BugScenario.NavigationReentry, value);

    partial void OnUiThreadBlockingEnabledChanged(bool value) =>
        UpdateScenario(BugScenario.UiThreadBlocking, value);

    partial void OnStateRaceEnabledChanged(bool value) =>
        UpdateScenario(BugScenario.StateRace, value);

    partial void OnSelectedTriggerModeIndexChanged(int value)
    {
        if (_synchronizingBugConfiguration || value < 0 || value > 2)
        {
            return;
        }

        _bugManager.SetTriggerMode((BugTriggerMode)value);
    }

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async cancellationToken =>
    {
        var summary = await _controller.GetDashboardAsync(cancellationToken);
        CourseCount = summary.CourseCount;
        BasketCount = summary.BasketCount;
        RegistrationCount = summary.RegistrationCount;
    });

    [RelayCommand]
    private async Task ResetAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Reset demo data?",
            "The basket and registration history will return to their initial test state.",
            "Reset",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        await RunBusyAsync(async cancellationToken =>
        {
            await _controller.ResetDemoAsync(cancellationToken);
            var summary = await _controller.GetDashboardAsync(cancellationToken);
            CourseCount = summary.CourseCount;
            BasketCount = summary.BasketCount;
            RegistrationCount = summary.RegistrationCount;
        });
    }

    [RelayCommand]
    private Task GoBackAsync() => _navigation.GoBackAsync(PageCancellationToken);

    [RelayCommand]
    private void EnableAllBugs()
    {
        _bugManager.EnableAll();
        SynchronizeBugConfiguration();
    }

    [RelayCommand]
    private void DisableAllBugs()
    {
        _bugManager.DisableAll();
        SynchronizeBugConfiguration();
    }

    private void UpdateScenario(BugScenario scenario, bool enabled)
    {
        if (_synchronizingBugConfiguration)
        {
            return;
        }

        _bugManager.SetScenarioEnabled(scenario, enabled);
        EnabledScenariosText = _bugManager.EnabledScenarios.ToString();
    }

    private void SynchronizeBugConfiguration()
    {
        _synchronizingBugConfiguration = true;

        try
        {
            CommandReentryEnabled = _bugManager.IsScenarioEnabled(BugScenario.CommandReentry);
            CollectionRaceEnabled = _bugManager.IsScenarioEnabled(BugScenario.CollectionRace);
            LifecycleEnabled = _bugManager.IsScenarioEnabled(BugScenario.Lifecycle);
            NavigationReentryEnabled = _bugManager.IsScenarioEnabled(BugScenario.NavigationReentry);
            UiThreadBlockingEnabled = _bugManager.IsScenarioEnabled(BugScenario.UiThreadBlocking);
            StateRaceEnabled = _bugManager.IsScenarioEnabled(BugScenario.StateRace);
            SelectedTriggerModeIndex = (int)_bugManager.TriggerMode;
            EnabledScenariosText = _bugManager.EnabledScenarios.ToString();
        }
        finally
        {
            _synchronizingBugConfiguration = false;
        }
    }
}
