using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    [QueryProperty(nameof(Mode), "mode")]
    public partial class LoginPage : ContentPage
    {
        private string? _mode;

        public string? Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                if (BindingContext is LoginViewModel vm)
                    vm.InitMode(value == "signup");
            }
        }

        public LoginPage(LoginViewModel viewModel)
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
