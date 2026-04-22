using CommunityToolkit.Mvvm.Input;
using kamee.app.Services;

namespace kamee.app.ViewModels
{
    public partial class SplashViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly SupabaseService _supabaseService;

        public SplashViewModel(AuthService authService, SupabaseService supabaseService)
        {
            _authService = authService;
            _supabaseService = supabaseService;
        }

        public async Task CheckSessionAsync()
        {
            await _supabaseService.InitializeAsync();
            if (_authService.IsLoggedIn)
                await NavigateToHomeAsync();
        }

        [RelayCommand]
        private async Task CreateAccount()
        {
            await Shell.Current.GoToAsync("//login?mode=signup");
        }

        [RelayCommand]
        private async Task SignIn()
        {
            await Shell.Current.GoToAsync("//login");
        }

        private static async Task NavigateToHomeAsync()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                await Shell.Current.GoToAsync("//homedesktop");
            else
                await Shell.Current.GoToAsync("//main/home");
        }
    }
}
