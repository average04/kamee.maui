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

        public async Task<List<User>> GetFriendsWatchingAsync(string userId)
        {
            var response = await _supabase.Client
                .From<User>()
                .Filter("current_room_id", Postgrest.Constants.Operator.NotEqual, string.Empty)
                .Get();
            return response.Models;
        }

        public async Task<Room?> CreateRoomAsync(string name, string platform, bool isPrivate)
        {
            var userId = _authService.GetCurrentUserId()
                ?? throw new InvalidOperationException("User not logged in");

            var room = new Room
            {
                Name = name,
                HostId = userId,
                StreamingPlatform = platform,
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

            // Update user's current_room_id
            await _supabase.Client
                .From<User>()
                .Filter("id", Postgrest.Constants.Operator.Equals, userId)
                .Set(u => u.CurrentRoomId, roomId)
                .Update();
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
        }
    }
}
