// Views/UserProfilePage.xaml.cs
using newRestaurant.ViewModels;

namespace newRestaurant.Views;

public partial class UserProfilePage : ContentPage
{
    public UserProfilePage(UserProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Load profile data when the page appears
        if (BindingContext is UserProfileViewModel vm)
        {
            vm.LoadUserProfile();
        }
    }
}