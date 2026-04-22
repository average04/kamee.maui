using kamee.app.ViewModels;

namespace kamee.app.Views.Desktop
{
    public partial class WatchRoomDesktopPage : ContentPage
    {
        public WatchRoomDesktopPage(WatchRoomViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, false);
            if (BindingContext is WatchRoomViewModel vm)
                await vm.LoadAsync();
        }
    }
}
