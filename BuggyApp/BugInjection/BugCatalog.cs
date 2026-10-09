namespace BuggyApp.BugInjection;

public sealed class BugCatalog
{
    private static readonly IReadOnlyList<BugDefinition> Definitions =
    [
        new(BugIds.CatalogToggleReentry, BugScenario.CommandReentry, "Catalog", "ToggleBasket", "Concurrent Add/Remove for one course."),
        new(BugIds.BasketRegisterReentry, BugScenario.CommandReentry, "Basket", "Register", "Concurrent registration submissions."),
        new(BugIds.HistoryDeleteReentry, BugScenario.CommandReentry, "History", "Delete", "Concurrent deletion of one registration."),
        new(BugIds.BasketRemoveClearRace, BugScenario.CollectionRace, "Basket", "Remove/Clear", "A delayed removal uses collection state invalidated by Clear All."),
        new(BugIds.HistoryDeleteClearRace, BugScenario.CollectionRace, "History", "Delete/Clear", "A delayed deletion uses collection state invalidated by Clear History."),
        new(BugIds.CatalogPagingRefreshRace, BugScenario.CollectionRace, "Catalog", "LoadMore/Refresh", "Paging and refresh update the course collection concurrently."),
        new(BugIds.CourseDetailAfterClose, BugScenario.Lifecycle, "CourseDetail", "Load", "A delayed course load completes after the page closes."),
        new(BugIds.RegistrationDetailAfterClose, BugScenario.Lifecycle, "RegistrationDetail", "Load", "A delayed registration load completes after the page closes."),
        new(BugIds.BasketRegisterAfterClose, BugScenario.Lifecycle, "Basket", "Register", "Registration completion navigates after the page closes."),
        new(BugIds.CatalogDetailsNavigationReentry, BugScenario.NavigationReentry, "Catalog", "OpenDetails", "Multiple course detail navigations overlap."),
        new(BugIds.HistoryDetailsNavigationReentry, BugScenario.NavigationReentry, "History", "OpenDetails", "Multiple registration detail navigations overlap."),
        new(BugIds.ConfirmationDialogReentry, BugScenario.NavigationReentry, "Shared", "Confirm", "Multiple confirmation dialogs overlap."),
        new(BugIds.DashboardRefreshAnr, BugScenario.UiThreadBlocking, "Dashboard", "Refresh", "Dashboard refresh blocks the UI thread."),
        new(BugIds.CatalogRefreshAnr, BugScenario.UiThreadBlocking, "Catalog", "Refresh", "Catalog refresh blocks the UI thread."),
        new(BugIds.BasketRegisterAnr, BugScenario.UiThreadBlocking, "Basket", "Register", "Registration blocks the UI thread.")
    ];

    private readonly IReadOnlyDictionary<string, BugDefinition> _byId =
        Definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);

    public IReadOnlyList<BugDefinition> GetAll() => Definitions;

    public BugDefinition GetRequired(string bugId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bugId);

        return _byId.TryGetValue(bugId, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Unknown injected bug ID '{bugId}'.");
    }
}
