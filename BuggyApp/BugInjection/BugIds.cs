namespace BuggyApp.BugInjection;

public static class BugIds
{
    public const string CatalogToggleReentry = "REENTRY-01";
    public const string BasketRegisterReentry = "REENTRY-02";
    public const string HistoryDeleteReentry = "REENTRY-03";

    public const string BasketRemoveClearRace = "COLLECTION-01";
    public const string HistoryDeleteClearRace = "COLLECTION-02";
    public const string CatalogPagingRefreshRace = "COLLECTION-03";

    public const string CourseDetailAfterClose = "LIFECYCLE-01";
    public const string RegistrationDetailAfterClose = "LIFECYCLE-02";
    public const string BasketRegisterAfterClose = "LIFECYCLE-03";

    public const string CatalogDetailsNavigationReentry = "NAV-01";
    public const string HistoryDetailsNavigationReentry = "NAV-02";
    public const string ConfirmationDialogReentry = "NAV-03";

    public const string DashboardRefreshAnr = "ANR-01";
    public const string CatalogRefreshAnr = "ANR-02";
    public const string BasketRegisterAnr = "ANR-03";
}
