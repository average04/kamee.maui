using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage(SplashViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, false);
            if (BindingContext is SplashViewModel vm)
                await vm.CheckSessionAsync();
        }
    }
}
