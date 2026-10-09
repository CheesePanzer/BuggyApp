using BuggyApp.ViewModels;

namespace BuggyApp.Pages;

public partial class MonkeyTestPage : ContentPage
{
    private readonly MonkeyTestViewModel _viewModel;

    public MonkeyTestPage(MonkeyTestViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.AppearingAsync();
    }

    protected override void OnDisappearing()
    {
        _viewModel.Disappearing();
        base.OnDisappearing();
    }
}
