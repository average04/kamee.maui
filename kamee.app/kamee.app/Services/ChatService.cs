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

            var messages = response.Models;
            var currentUserId = _authService.GetCurrentUserId();

            var userIds = messages.Select(m => m.UserId).Distinct().ToList();
            var profiles = await FetchProfilesAsync(userIds);

            foreach (var msg in messages)
            {
                msg.IsFromCurrentUser = msg.UserId == currentUserId;
                if (profiles.TryGetValue(msg.UserId, out var profile))
                {
                    msg.Username = profile.Username;
                    msg.AvatarInitials = profile.AvatarInitials;
                }
            }

            return messages;
        }

        private async Task<Dictionary<string, User>> FetchProfilesAsync(List<string> userIds)
        {
            if (userIds.Count == 0) return new Dictionary<string, User>();

            var csv = string.Join(",", userIds);
            var response = await _supabase.Client
                .From<User>()
                .Filter("id", Postgrest.Constants.Operator.In, $"({csv})")
                .Get();

            return response.Models.ToDictionary(u => u.Id);
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

            _channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.Inserts, async (_, change) =>
            {
                if (change.Model<Message>() is not Message msg) return;

                msg.IsFromCurrentUser = msg.UserId == _authService.GetCurrentUserId();

                try
                {
                    var profileResponse = await _supabase.Client
                        .From<User>()
                        .Filter("id", Postgrest.Constants.Operator.Equals, msg.UserId)
                        .Single();

                    if (profileResponse != null)
                    {
                        msg.Username = profileResponse.Username;
                        msg.AvatarInitials = profileResponse.AvatarInitials;
                    }
                }
                catch { /* username stays empty on failure */ }

                onMessage(msg);
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
