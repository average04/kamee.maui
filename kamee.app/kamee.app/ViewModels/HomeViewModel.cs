using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kamee.app.Models;
using kamee.app.Services;

namespace kamee.app.ViewModels
{
    public partial class HomeViewModel : BaseViewModel
    {
        private readonly RoomService _roomService;
        private readonly AuthService _authService;

        [ObservableProperty]
        private ObservableCollection<Room> _liveRooms = new();

        [ObservableProperty]
        private ObservableCollection<User> _friendsWatching = new();

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private string _activeTab = "For you";

        public HomeViewModel(RoomService roomService, AuthService authService)
        {
            _roomService = roomService;
            _authService = authService;
        }

        [ObservableProperty]
        private string? _errorMessage;

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                CurrentUser = await _authService.GetCurrentUserAsync();

                var rooms = await _roomService.GetLiveRoomsAsync();
                LiveRooms = new ObservableCollection<Room>(rooms);

                if (CurrentUser?.Id != null)
                {
                    var friends = await _roomService.GetFriendsWatchingAsync(CurrentUser.Id);
                    FriendsWatching = new ObservableCollection<User>(friends);
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
        private async Task JoinRoom(Room room)
        {
            var userId = _authService.GetCurrentUserId();
            if (userId == null) return;

            await _roomService.JoinRoomAsync(room.Id, userId);

            if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                await Shell.Current.GoToAsync($"watchroomdesktop?roomId={room.Id}");
            else
                await Shell.Current.GoToAsync($"watchroom?roomId={room.Id}");
        }

        [RelayCommand]
        private async Task OpenCreateRoom()
        {
            await Shell.Current.GoToAsync("createroom");
        }

        [RelayCommand]
        private void SetActiveTab(string tab)
        {
            ActiveTab = tab;
        }
    }
}
