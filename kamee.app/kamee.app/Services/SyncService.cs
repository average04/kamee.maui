using Newtonsoft.Json;
using Supabase.Realtime;
using static Supabase.Realtime.Constants;
using Supabase.Realtime.Models;

namespace kamee.app.Services
{
    class SyncPayload
    {
        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;

        [JsonProperty("position")]
        public double Position { get; set; }

        [JsonProperty("sender_id")]
        public string SenderId { get; set; } = string.Empty;

        [JsonProperty("video_id")]
        public string VideoId { get; set; } = string.Empty;
    }

    class SyncBroadcast : BaseBroadcast<SyncPayload> { }

    public class SyncService
    {
        private readonly SupabaseService _supabase;
        private readonly AuthService _authService;
        private RealtimeChannel? _channel;
        private RealtimeBroadcast<SyncBroadcast>? _broadcast;

        public SyncService(SupabaseService supabase, AuthService authService)
        {
            _supabase = supabase;
            _authService = authService;
        }

        public async Task BroadcastPlayAsync(string roomId, double position)
        {
            if (_channel == null) return;
            await _channel.Send(ChannelEventName.Broadcast, "sync",
                new { action = "play", position, sender_id = _authService.GetCurrentUserId() ?? string.Empty }, 5000);
        }

        public async Task BroadcastPauseAsync(string roomId, double position)
        {
            if (_channel == null) return;
            await _channel.Send(ChannelEventName.Broadcast, "sync",
                new { action = "pause", position, sender_id = _authService.GetCurrentUserId() ?? string.Empty }, 5000);
        }

        public async Task BroadcastSeekAsync(string roomId, double position)
        {
            if (_channel == null) return;
            await _channel.Send(ChannelEventName.Broadcast, "sync",
                new { action = "seek", position, sender_id = _authService.GetCurrentUserId() ?? string.Empty }, 5000);
        }

        public async Task BroadcastVideoChangedAsync(string roomId, string videoId)
        {
            if (_channel == null) return;
            await _channel.Send(ChannelEventName.Broadcast, "sync",
                new { action = "video_changed", video_id = videoId, position = 0.0, sender_id = _authService.GetCurrentUserId() ?? string.Empty }, 5000);
        }

        public async Task SubscribeToSyncAsync(
            string roomId,
            Action<double> onPlay,
            Action<double> onPause,
            Action<double> onSeek,
            Action<string>? onVideoChanged = null)
        {
            _channel = _supabase.Client.Realtime.Channel($"room:{roomId}");
            _broadcast = _channel.Register<SyncBroadcast>(false, false);

            _broadcast.AddBroadcastEventHandler((_, _) =>
            {
                var current = _broadcast?.Current();
                if (current?.Payload == null) return;

                // Ignore events broadcast by this client to prevent echo loops
                var currentUserId = _authService.GetCurrentUserId() ?? string.Empty;
                if (!string.IsNullOrEmpty(current.Payload.SenderId) &&
                    current.Payload.SenderId == currentUserId) return;

                switch (current.Payload.Action)
                {
                    case "play": onPlay(current.Payload.Position); break;
                    case "pause": onPause(current.Payload.Position); break;
                    case "seek": onSeek(current.Payload.Position); break;
                    case "video_changed":
                        if (onVideoChanged != null && !string.IsNullOrEmpty(current.Payload.VideoId))
                            onVideoChanged(current.Payload.VideoId);
                        break;
                }
            });

            await _channel.Subscribe();
        }

        public Task UnsubscribeAsync()
        {
            _channel?.Unsubscribe();
            _channel = null;
            _broadcast = null;
            return Task.CompletedTask;
        }
    }
}
