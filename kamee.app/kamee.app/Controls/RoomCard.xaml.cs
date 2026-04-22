using kamee.app.Models;

namespace kamee.app.Controls
{
    public partial class RoomCard : ContentView
    {
        public static readonly BindableProperty RoomProperty =
            BindableProperty.Create(nameof(Room), typeof(Room), typeof(RoomCard));

        public static readonly BindableProperty TapCommandProperty =
            BindableProperty.Create(nameof(TapCommand), typeof(System.Windows.Input.ICommand), typeof(RoomCard));

        public Room? Room
        {
            get => (Room?)GetValue(RoomProperty);
            set => SetValue(RoomProperty, value);
        }

        public System.Windows.Input.ICommand? TapCommand
        {
            get => (System.Windows.Input.ICommand?)GetValue(TapCommandProperty);
            set => SetValue(TapCommandProperty, value);
        }

        public RoomCard()
        {
            InitializeComponent();
        }
    }
}
