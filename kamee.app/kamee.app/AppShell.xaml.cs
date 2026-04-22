namespace kamee.app
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("watchroom", typeof(Views.Mobile.WatchRoomPage));
            Routing.RegisterRoute("watchroomdesktop", typeof(Views.Desktop.WatchRoomDesktopPage));
            Routing.RegisterRoute("createroom", typeof(Views.Mobile.CreateRoomPage));
        }
    }
}
