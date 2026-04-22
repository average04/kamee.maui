namespace kamee.app.Services
{
    public class SupabaseService
    {
        private Supabase.Client? _client;

        public Supabase.Client Client => _client
            ?? throw new InvalidOperationException("SupabaseService not initialized. Call InitializeAsync first.");

        public async Task InitializeAsync()
        {
            if (_client != null) return;

            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };
            _client = new Supabase.Client(Constants.SupabaseUrl, Constants.SupabaseAnonKey, options);
            await _client.InitializeAsync();
        }
    }
}
