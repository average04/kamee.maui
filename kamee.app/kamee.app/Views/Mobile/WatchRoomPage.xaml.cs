using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    public partial class WatchRoomPage : ContentPage
    {
        public WatchRoomPage(WatchRoomViewModel viewModel)
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
