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
        private readonly RoomService _roomService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ObservableCollection<WatchHistory> _watchHistory = new();

        [ObservableProperty]
        private int _roomCount;

        [ObservableProperty]
        private string? _errorMessage;

        public ProfileViewModel(AuthService authService, RoomService roomService)
        {
            _authService = authService;
            _roomService = roomService;
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                CurrentUser = await _authService.GetCurrentUserAsync();

                if (CurrentUser?.Id != null)
                {
                    var history = await _roomService.GetWatchHistoryAsync(CurrentUser.Id);
                    WatchHistory = new ObservableCollection<WatchHistory>(history);

                    RoomCount = await _roomService.GetRoomCountAsync(CurrentUser.Id);
                }
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
        private async Task SignOut()
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//splash");
        }
    }
}
