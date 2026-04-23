using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    [QueryProperty(nameof(RoomId), "roomId")]
    public partial class WatchRoomPage : ContentPage
    {
        private string _roomId = string.Empty;
        public string RoomId
        {
            get => _roomId;
            set
            {
                _roomId = value;
                if (BindingContext is WatchRoomViewModel vm)
                    vm.RoomId = value;
            }
        }

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

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is WatchRoomViewModel vm)
                await vm.CleanupAsync();
        }
    }
}
