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
    }

    class SyncBroadcast : BaseBroadcast<SyncPayload> { }

    public class SyncService
    {
        private readonly SupabaseService _supabase;
        private RealtimeChannel? _channel;
        private RealtimeBroadcast<SyncBroadcast>? _broadcast;

        public SyncService(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        public async Task BroadcastPlayAsync(string roomId, double position)
        {
            if (_channel == null) return;
            await _channel.Send(ChannelEventName.Broadcast, "sync",
                new { action = "play", position }, 5000);
        }

        public async Task BroadcastPauseAsync(string roomId, double position)
        {
            if (_channel == null) return;
            await _channel.Send(ChannelEventName.Broadcast, "sync",
                new { action = "pause", position }, 5000);
        }

        public async Task BroadcastSeekAsync(string roomId, double position)
        {
            if (_channel == null) return;
            await _channel.Send(ChannelEventName.Broadcast, "sync",
                new { action = "seek", position }, 5000);
        }

        public async Task SubscribeToSyncAsync(
            string roomId,
            Action<double> onPlay,
            Action<double> onPause,
            Action<double> onSeek)
        {
            _channel = _supabase.Client.Realtime.Channel($"room:{roomId}");
            _broadcast = _channel.Register<SyncBroadcast>(false, false);

            _broadcast.AddBroadcastEventHandler((_, _) =>
            {
                var current = _broadcast?.Current();
                if (current?.Payload == null) return;

                switch (current.Payload.Action)
                {
                    case "play": onPlay(current.Payload.Position); break;
                    case "pause": onPause(current.Payload.Position); break;
                    case "seek": onSeek(current.Payload.Position); break;
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
