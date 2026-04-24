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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(YoutubeVideoId))]
        private string _videoUrl = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotHost))]
        private bool _isHost;

        public bool IsNotHost => !_isHost;

        public string YoutubeVideoId => ExtractYoutubeId(VideoUrl);

        private static readonly System.Text.RegularExpressions.Regex YoutubeIdRegex = new(
            @"(?:youtube\.com/watch\?.*v=|youtu\.be/|youtube\.com/embed/|youtube\.com/shorts/)([a-zA-Z0-9_-]{11})",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        private static string ExtractYoutubeId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            var match = YoutubeIdRegex.Match(url);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        // Set by the page after LoadAsync so remote sync events execute JS on the player.
        public Action<double>? OnRemotePlay { get; set; }
        public Action<double>? OnRemotePause { get; set; }
        public Action<double>? OnRemoteSeek { get; set; }
        public Action<string>? OnRemoteVideoChanged { get; set; }

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
                VideoUrl = room?.VideoUrl ?? string.Empty;
                IsHost = room?.HostId == _authService.GetCurrentUserId();

                var messages = await _chatService.GetMessagesAsync(RoomId);
                Messages = new ObservableCollection<Message>(messages);

                await _chatService.SubscribeToRoomAsync(RoomId, OnMessageReceived);

                await _syncService.SubscribeToSyncAsync(
                    RoomId,
                    position => MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PlaybackPosition = position;
                        IsPlaying = true;
                        OnRemotePlay?.Invoke(position);
                    }),
                    position => MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PlaybackPosition = position;
                        IsPlaying = false;
                        OnRemotePause?.Invoke(position);
                    }),
                    position => MainThread.BeginInvokeOnMainThread(() =>
                    {
                        PlaybackPosition = position;
                        OnRemoteSeek?.Invoke(position);
                    }),
                    videoId => MainThread.BeginInvokeOnMainThread(() =>
                    {
                        VideoUrl = $"https://www.youtube.com/watch?v={videoId}";
                        OnRemoteVideoChanged?.Invoke(videoId);
                    }));
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

        public Task BroadcastPlayAsync(double position) =>
            _syncService.BroadcastPlayAsync(RoomId, position);

        public Task BroadcastPauseAsync(double position) =>
            _syncService.BroadcastPauseAsync(RoomId, position);

        public async Task BroadcastVideoChangedAsync(string videoId)
        {
            VideoUrl = $"https://www.youtube.com/watch?v={videoId}";
            await _roomService.UpdateVideoUrlAsync(RoomId, VideoUrl);
            await _syncService.BroadcastVideoChangedAsync(RoomId, videoId);
        }

        public async Task CleanupAsync()
        {
            await _chatService.UnsubscribeAsync();
            await _syncService.UnsubscribeAsync();
            _isLoaded = false;
            OnRemotePlay = null;
            OnRemotePause = null;
            OnRemoteSeek = null;
            OnRemoteVideoChanged = null;
        }

        [RelayCommand]
        private async Task LeaveRoom()
        {
            var userId = _authService.GetCurrentUserId();
            if (userId != null)
                await _roomService.LeaveRoomAsync(RoomId, userId);

            await CleanupAsync();
            await Shell.Current.GoToAsync("..");
        }
    }
}
