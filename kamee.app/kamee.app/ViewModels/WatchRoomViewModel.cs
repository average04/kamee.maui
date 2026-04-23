using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kamee.app.Models;
using kamee.app.Services;

namespace kamee.app.ViewModels
{
    public partial class WatchRoomViewModel : BaseViewModel
    {
        private readonly ChatService _chatService;
        private readonly SyncService _syncService;
        private readonly RoomService _roomService;
        private readonly AuthService _authService;
        private bool _isLoaded;

        [ObservableProperty]
        private string _roomId = string.Empty;

        [ObservableProperty]
        private string _roomName = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Message> _messages = new();

        [ObservableProperty]
        private string _messageInput = string.Empty;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private double _playbackPosition;

        [ObservableProperty]
        private ObservableCollection<User> _viewers = new();

        public WatchRoomViewModel(
            ChatService chatService,
            SyncService syncService,
            RoomService roomService,
            AuthService authService)
        {
            _chatService = chatService;
            _syncService = syncService;
            _roomService = roomService;
            _authService = authService;
        }

        public async Task LoadAsync()
        {
            if (string.IsNullOrEmpty(RoomId)) return;
            if (_isLoaded) return;
            _isLoaded = true;
            IsBusy = true;

            try
            {
                var room = await _roomService.GetRoomAsync(RoomId);
                RoomName = room?.Name ?? string.Empty;

                var messages = await _chatService.GetMessagesAsync(RoomId);
                Messages = new ObservableCollection<Message>(messages);

                await _chatService.SubscribeToRoomAsync(RoomId, OnMessageReceived);

                await _syncService.SubscribeToSyncAsync(
                    RoomId,
                    position => { PlaybackPosition = position; IsPlaying = true; },
                    position => { PlaybackPosition = position; IsPlaying = false; },
                    position => { PlaybackPosition = position; });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WatchRoom load error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnMessageReceived(Message message)
        {
            MainThread.BeginInvokeOnMainThread(() => Messages.Add(message));
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageInput)) return;
            var content = MessageInput;
            MessageInput = string.Empty;
            await _chatService.SendMessageAsync(RoomId, content);
        }

        [RelayCommand]
        private async Task TogglePlay()
        {
            IsPlaying = !IsPlaying;
            if (IsPlaying)
                await _syncService.BroadcastPlayAsync(RoomId, PlaybackPosition);
            else
                await _syncService.BroadcastPauseAsync(RoomId, PlaybackPosition);
        }

        public async Task CleanupAsync()
        {
            await _chatService.UnsubscribeAsync();
            await _syncService.UnsubscribeAsync();
            _isLoaded = false;
        }

        [RelayCommand]
        private async Task LeaveRoom()
        {
            var userId = _authService.GetCurrentUserId();
            if (userId != null)
                await _roomService.LeaveRoomAsync(RoomId, userId);

            await _chatService.UnsubscribeAsync();
            await _syncService.UnsubscribeAsync();
            await Shell.Current.GoToAsync("..");
        }
    }
}
