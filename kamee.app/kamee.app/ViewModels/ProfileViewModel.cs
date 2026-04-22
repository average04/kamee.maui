using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kamee.app.Models;
using kamee.app.Services;

namespace kamee.app.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ObservableCollection<WatchHistory> _watchHistory = new();

        public ProfileViewModel(AuthService authService)
        {
            _authService = authService;
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                CurrentUser = await _authService.GetCurrentUserAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SignOut()
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//splash");
        }
    }
}
