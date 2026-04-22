using kamee.app.ViewModels;

namespace kamee.app.Views.Desktop
{
    public partial class HomeDesktopPage : ContentPage
    {
        public HomeDesktopPage(HomeViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, false);
            if (BindingContext is HomeViewModel vm)
                await vm.LoadAsync();
        }
    }
}
