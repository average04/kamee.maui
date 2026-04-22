using kamee.app.Models;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;

namespace kamee.app.Services
{
    public class ChatService
    {
        private readonly SupabaseService _supabase;
        private readonly AuthService _authService;
        private RealtimeChannel? _channel;

        public ChatService(SupabaseService supabase, AuthService authService)
        {
            _supabase = supabase;
            _authService = authService;
        }

        public async Task<List<Message>> GetMessagesAsync(string roomId)
        {
            var response = await _supabase.Client
                .From<Message>()
                .Filter("room_id", Postgrest.Constants.Operator.Equals, roomId)
                .Order("sent_at", Postgrest.Constants.Ordering.Ascending)
                .Get();

            var currentUserId = _authService.GetCurrentUserId();
            foreach (var msg in response.Models)
                msg.IsFromCurrentUser = msg.UserId == currentUserId;

            return response.Models;
        }

        public async Task SendMessageAsync(string roomId, string content)
        {
            var userId = _authService.GetCurrentUserId()
                ?? throw new InvalidOperationException("User not logged in");

            var message = new Message
            {
                RoomId = roomId,
                UserId = userId,
                Content = content,
                SentAt = DateTime.UtcNow
            };
            await _supabase.Client.From<Message>().Insert(message);
        }

        public async Task SubscribeToRoomAsync(string roomId, Action<Message> onMessage)
        {
            _channel = _supabase.Client.Realtime.Channel($"room-messages:{roomId}");

            _channel.Register(new PostgresChangesOptions(
                "public",
                "messages",
                PostgresChangesOptions.ListenType.Inserts,
                $"room_id=eq.{roomId}"));

            _channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.Inserts, (_, change) =>
            {
                if (change.Model<Message>() is Message msg)
                {
                    msg.IsFromCurrentUser = msg.UserId == _authService.GetCurrentUserId();
                    onMessage(msg);
                }
            });

            await _channel.Subscribe();
        }

        public Task UnsubscribeAsync()
        {
            _channel?.Unsubscribe();
            _channel = null;
            return Task.CompletedTask;
        }
    }
}
