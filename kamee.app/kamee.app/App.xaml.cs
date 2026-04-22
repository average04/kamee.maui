namespace kamee.app
{
    public partial class App : Application
    {
        private readonly Services.SupabaseService _supabaseService;

        public App(Services.SupabaseService supabaseService)
        {
            InitializeComponent();
            _supabaseService = supabaseService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override async void OnStart()
        {
            base.OnStart();
            await _supabaseService.InitializeAsync();
        }
    }
}
