using BuggyApp.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace BuggyApp;

public partial class AppShell : Shell
{
    public AppShell(
        IServiceProvider services)
    {
        InitializeComponent();

        MainTabBar.Items.Add(CreateTab<DashboardPage>("Home", "home", services));
        MainTabBar.Items.Add(CreateTab<CatalogPage>("Catalog", "catalog", services));
        MainTabBar.Items.Add(CreateTab<BasketPage>("Basket", "basket", services));
        MainTabBar.Items.Add(CreateTab<HistoryPage>("History", "history", services));

        Routing.RegisterRoute(
            "course-detail",
            new ServiceRouteFactory(services, typeof(CourseDetailPage)));
        Routing.RegisterRoute(
            "registration-result",
            new ServiceRouteFactory(services, typeof(RegistrationResultPage)));
        Routing.RegisterRoute(
            "registration-detail",
            new ServiceRouteFactory(services, typeof(RegistrationDetailPage)));
    }

    private static ShellContent CreateTab<TPage>(
        string title,
        string route,
        IServiceProvider services)
        where TPage : Page =>
        new()
        {
            Title = title,
            Route = route,
            ContentTemplate = new DataTemplate(() =>
                services.GetRequiredService<TPage>())
        };
}
