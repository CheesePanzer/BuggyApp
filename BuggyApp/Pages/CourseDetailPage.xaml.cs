using BuggyApp.ViewModels;

namespace BuggyApp.Pages;

public partial class CourseDetailPage : ContentPage, IQueryAttributable
{
    private readonly CourseDetailViewModel _viewModel;

    public CourseDetailPage(CourseDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("CourseId", out var value) && value is int courseId)
        {
            _viewModel.ApplyCourseId(courseId);
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
