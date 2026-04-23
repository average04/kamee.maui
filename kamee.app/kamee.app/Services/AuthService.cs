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
            // Pass username in metadata so the DB trigger picks it up.
            // The trigger runs as security definer and creates the profile,
            // avoiding any RLS issues with an unauthenticated client.
            var options = new Supabase.Gotrue.SignUpOptions
            {
                Data = new Dictionary<string, object> { ["username"] = username }
            };
            return await _supabase.Client.Auth.SignUp(email, password, options);
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
