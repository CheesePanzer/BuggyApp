using BuggyApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuggyApp.ViewModels;

public partial class CourseItemViewModel : ObservableObject
{
    public CourseItemViewModel(Course course, bool isInBasket)
    {
        Course = course;
        IsInBasket = isInBasket;
    }

    public Course Course { get; }

    public int Id => Course.Id;

    public string Code => Course.Code;

    public string Title => Course.Title;

    public string Category => Course.Category;

    public string Instructor => Course.Instructor;

    public string Description => Course.Description;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionText))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    public partial bool IsInBasket { get; set; }

    [ObservableProperty]
    public partial bool IsUpdating { get; set; }

    public string ActionText => IsInBasket ? "Remove" : "Add";

    public string StatusText => IsInBasket ? "In basket" : "Available";
}
