using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    public partial class CreateRoomPage : ContentPage
    {
        public CreateRoomPage(CreateRoomViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, false);
        }
    }
}
