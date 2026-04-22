using kamee.app.Models;

namespace kamee.app.Services
{
    public class AuthService
    {
        private readonly SupabaseService _supabase;

        public AuthService(SupabaseService supabase)
        {
            _supabase = supabase;
        }

        public bool IsLoggedIn => _supabase.Client.Auth.CurrentUser != null;

        public string? GetCurrentUserId() => _supabase.Client.Auth.CurrentUser?.Id;

        public async Task<Supabase.Gotrue.Session?> SignUpAsync(string email, string password, string username)
        {
            var session = await _supabase.Client.Auth.SignUp(email, password);
            if (session?.User != null)
            {
                var profile = new User
                {
                    Id = session.User.Id ?? string.Empty,
                    Username = username,
                    CreatedAt = DateTime.UtcNow
                };
                await _supabase.Client.From<User>().Insert(profile);
            }
            return session;
        }

        public async Task<Supabase.Gotrue.Session?> SignInAsync(string email, string password)
        {
            return await _supabase.Client.Auth.SignIn(email, password);
        }

        public async Task SignOutAsync()
        {
            await _supabase.Client.Auth.SignOut();
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var authUser = _supabase.Client.Auth.CurrentUser;
            if (authUser?.Id == null) return null;

            var response = await _supabase.Client
                .From<User>()
                .Filter("id", Postgrest.Constants.Operator.Equals, authUser.Id)
                .Single();

            if (response != null)
                response.Email = authUser.Email ?? string.Empty;

            return response;
        }
    }
}
