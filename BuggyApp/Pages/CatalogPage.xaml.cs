using BuggyApp.ViewModels;

namespace BuggyApp.Pages;

public partial class CatalogPage : ContentPage
{
    private readonly CatalogViewModel _viewModel;

    public CatalogPage(CatalogViewModel viewModel)
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
