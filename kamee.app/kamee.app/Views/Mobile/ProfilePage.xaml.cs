using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage(ProfileViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, false);
            if (BindingContext is ProfileViewModel vm)
                await vm.LoadAsync();
        }
    }
}
