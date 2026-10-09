using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using BuggyApp.Configuration;
using BuggyApp.Data;
using BuggyApp.Services;
using BuggyApp.Pages;
using BuggyApp.ViewModels;

namespace BuggyApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialSymbolsRounded.ttf", "MaterialSymbols");
                });

            builder.Services.AddSingleton(new MockServerOptions());
            builder.Services.AddSingleton<MockedDb>();
            builder.Services.AddSingleton<IMockDelayProvider, MockDelayProvider>();
            builder.Services.AddSingleton<DiagnosticsService>();
            builder.Services.AddSingleton<IMockedCourseController, MockedCourseController>();
            builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
            builder.Services.AddSingleton<IDialogService, DialogService>();

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<CatalogPage>();
            builder.Services.AddTransient<BasketPage>();
            builder.Services.AddTransient<CourseDetailPage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<RegistrationResultPage>();
            builder.Services.AddTransient<RegistrationDetailPage>();
            builder.Services.AddTransient<MonkeyTestPage>();

            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<CatalogViewModel>();
            builder.Services.AddTransient<BasketViewModel>();
            builder.Services.AddTransient<CourseDetailViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<RegistrationResultViewModel>();
            builder.Services.AddTransient<RegistrationDetailViewModel>();
            builder.Services.AddTransient<MonkeyTestViewModel>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
