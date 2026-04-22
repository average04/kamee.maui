using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kamee.app.Services;

namespace kamee.app.ViewModels
{
    public partial class CreateRoomViewModel : BaseViewModel
    {
        private readonly RoomService _roomService;

        [ObservableProperty]
        private string _roomName = string.Empty;

        [ObservableProperty]
        private string _selectedPlatform = string.Empty;

        [ObservableProperty]
        private bool _isPrivate;

        [ObservableProperty]
        private string? _errorMessage;

        public List<string> Platforms { get; } = new()
        {
            "Netflix 📺", "YouTube ▶️", "Disney+ 🎬",
            "Prime 🎥", "HBO Max 📽️", "Custom URL 🔗"
        };

        public CreateRoomViewModel(RoomService roomService)
        {
            _roomService = roomService;
        }

        [RelayCommand]
        private void SelectPlatform(string platform)
        {
            SelectedPlatform = platform;
        }

        [RelayCommand]
        private async Task Create()
        {
            if (string.IsNullOrWhiteSpace(RoomName))
            {
                ErrorMessage = "Room name is required";
                return;
            }

            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var room = await _roomService.CreateRoomAsync(RoomName, SelectedPlatform, IsPrivate);
                if (room != null)
                {
                    if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
                        await Shell.Current.GoToAsync($"watchroomdesktop?roomId={room.Id}");
                    else
                        await Shell.Current.GoToAsync($"watchroom?roomId={room.Id}");
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
        private static async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
