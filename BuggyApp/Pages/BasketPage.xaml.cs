using BuggyApp.ViewModels;

namespace BuggyApp.Pages;

public partial class BasketPage : ContentPage
{
    private readonly BasketViewModel _viewModel;

    public BasketPage(BasketViewModel viewModel)
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
