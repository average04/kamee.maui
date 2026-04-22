using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    public partial class HomePage : ContentPage
    {
        public HomePage(HomeViewModel viewModel)
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
