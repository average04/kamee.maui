using kamee.app.Models;

namespace kamee.app.Services
{
    public class AuthService
    {
        private readonly SupabaseService _supabase;
        private string? _cachedUserId;
        private string? _cachedEmail;

        public AuthService(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        public bool IsLoggedIn =>
            _supabase.Client.Auth.CurrentUser != null || _cachedUserId != null;

        public string? GetCurrentUserId() =>
            _supabase.Client.Auth.CurrentUser?.Id ?? _cachedUserId;

        public async Task<Supabase.Gotrue.Session?> SignUpAsync(string email, string password, string username)
        {
            var options = new Supabase.Gotrue.SignUpOptions
            {
                Data = new Dictionary<string, object> { ["username"] = username }
            };
            var session = await _supabase.Client.Auth.SignUp(email, password, options);

            if (session?.AccessToken == null)
                throw new Exception("Account created! Check your email to confirm it, then sign in.");

            _cachedUserId = session.User?.Id;
            _cachedEmail = email;
            return session;
        }

        public async Task<Supabase.Gotrue.Session?> SignInAsync(string email, string password)
        {
            var session = await _supabase.Client.Auth.SignIn(email, password);
            _cachedUserId = session?.User?.Id;
            _cachedEmail = email;
            return session;
        }

        public async Task SignOutAsync()
        {
            _cachedUserId = null;
            _cachedEmail = null;
            await _supabase.Client.Auth.SignOut();
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return null;

            var authUser = _supabase.Client.Auth.CurrentUser;

            var response = await _supabase.Client
                .From<User>()
                .Filter("id", Postgrest.Constants.Operator.Equals, userId)
                .Single();

            if (response != null)
                response.Email = authUser?.Email ?? _cachedEmail ?? string.Empty;

            return response;
        }
    }
}
