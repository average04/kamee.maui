using kamee.app.Models;

namespace kamee.app.Services
{
    public class RoomService
    {
        private readonly SupabaseService _supabase;
        private readonly AuthService _authService;

        public RoomService(SupabaseService supabase, AuthService authService)
        {
            _supabase = supabase;
            _authService = authService;
        }

        public async Task<List<Room>> GetLiveRoomsAsync()
        {
            var response = await _supabase.Client
                .From<Room>()
                .Filter("is_live", Postgrest.Constants.Operator.Equals, true)
                .Get();
            return response.Models;
        }

        public async Task<Room?> GetRoomAsync(string roomId)
        {
            var response = await _supabase.Client
                .From<Room>()
                .Filter("id", Postgrest.Constants.Operator.Equals, roomId)
                .Single();
            return response;
        }

        public async Task<List<User>> GetFriendsWatchingAsync(string userId)
        {
            var response = await _supabase.Client
                .From<User>()
                .Filter("current_room_id", Postgrest.Constants.Operator.NotEqual, string.Empty)
                .Get();
            return response.Models;
        }

        public async Task<Room?> CreateRoomAsync(string name, string platform, string? videoUrl, bool isPrivate)
        {
            var userId = _authService.GetCurrentUserId()
                ?? throw new InvalidOperationException("User not logged in");

            var room = new Room
            {
                Name = name,
                HostId = userId,
                StreamingPlatform = platform,
                VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl.Trim(),
                IsPrivate = isPrivate,
                IsLive = true,
                CreatedAt = DateTime.UtcNow
            };

            var response = await _supabase.Client.From<Room>().Insert(room);
            return response.Model;
        }

        public async Task JoinRoomAsync(string roomId, string userId)
        {
            var member = new RoomMember
            {
                RoomId = roomId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };
            await _supabase.Client.From<RoomMember>().Insert(member);

            await _supabase.Client
                .From<User>()
                .Filter("id", Postgrest.Constants.Operator.Equals, userId)
                .Set(u => u.CurrentRoomId, roomId)
                .Update();

            await _supabase.Client.Rpc("increment_viewer_count",
                new Dictionary<string, object> { ["room_id"] = roomId });
        }

        public async Task LeaveRoomAsync(string roomId, string userId)
        {
            await _supabase.Client
                .From<RoomMember>()
                .Filter("room_id", Postgrest.Constants.Operator.Equals, roomId)
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId)
                .Delete();

            await _supabase.Client
                .From<User>()
                .Filter("id", Postgrest.Constants.Operator.Equals, userId)
                .Set(u => u.CurrentRoomId, (string?)null)
                .Update();

            await _supabase.Client.Rpc("decrement_viewer_count",
                new Dictionary<string, object> { ["room_id"] = roomId });
        }

        public async Task UpdateVideoUrlAsync(string roomId, string videoUrl)
        {
            await _supabase.Client
                .From<Room>()
                .Filter("id", Postgrest.Constants.Operator.Equals, roomId)
                .Set(r => r.VideoUrl, videoUrl)
                .Update();
        }

        public async Task<List<WatchHistory>> GetWatchHistoryAsync(string userId)
        {
            var response = await _supabase.Client
                .From<WatchHistory>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId)
                .Order("watched_at", Postgrest.Constants.Ordering.Descending)
                .Limit(20)
                .Get();
            return response.Models;
        }

        public async Task<int> GetRoomCountAsync(string userId)
        {
            var response = await _supabase.Client
                .From<RoomMember>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId)
                .Get();
            return response.Models.Count;
        }
    }
}
