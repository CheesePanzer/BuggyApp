using BuggyApp.ViewModels;

namespace BuggyApp.Pages;

public partial class RegistrationDetailPage : ContentPage, IQueryAttributable
{
    private readonly RegistrationDetailViewModel _viewModel;

    public RegistrationDetailPage(RegistrationDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("RegistrationId", out var value) && value is Guid registrationId)
        {
            _viewModel.ApplyRegistrationId(registrationId);
        }
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
