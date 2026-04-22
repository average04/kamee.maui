using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kamee.app.Services;

namespace kamee.app.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private bool _isSignUpMode;

        [ObservableProperty]
        private bool _isPasswordVisible;

        [ObservableProperty]
        private string? _errorMessage;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
        }

        public void InitMode(bool signUp)
        {
            IsSignUpMode = signUp;
            ErrorMessage = null;
        }

        [RelayCommand]
        private async Task Submit()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                if (IsSignUpMode)
                {
                    if (string.IsNullOrWhiteSpace(Username))
                    {
                        ErrorMessage = "Username is required";
                        return;
                    }
                    await _authService.SignUpAsync(Email, Password, Username);
                }
                else
                {
                    await _authService.SignInAsync(Email, Password);
                }

                if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                    await Shell.Current.GoToAsync("//homedesktop");
                else
                    await Shell.Current.GoToAsync("//main/home");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private static async Task GoBack()
        {
            await Shell.Current.GoToAsync("//splash");
        }

        [RelayCommand]
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        [RelayCommand]
        private void ToggleMode()
        {
            IsSignUpMode = !IsSignUpMode;
            ErrorMessage = null;
        }
    }
}
