namespace kamee.app.Controls
{
    public partial class AvatarControl : ContentView
    {
        public static readonly BindableProperty InitialsProperty =
            BindableProperty.Create(nameof(Initials), typeof(string), typeof(AvatarControl), "??");

        public static readonly BindableProperty AvatarColorProperty =
            BindableProperty.Create(nameof(AvatarColor), typeof(Color), typeof(AvatarControl), Color.FromArgb("#C4B0FF"));

        public static readonly BindableProperty SizeProperty =
            BindableProperty.Create(nameof(Size), typeof(double), typeof(AvatarControl), 36.0);

        public static readonly BindableProperty FontSizeProperty =
            BindableProperty.Create(nameof(FontSize), typeof(double), typeof(AvatarControl), 13.0);

        public static readonly BindableProperty IsOnlineProperty =
            BindableProperty.Create(nameof(IsOnline), typeof(bool), typeof(AvatarControl), false);

        public string Initials
        {
            get => (string)GetValue(InitialsProperty);
            set => SetValue(InitialsProperty, value);
        }

        public Color AvatarColor
        {
            get => (Color)GetValue(AvatarColorProperty);
            set => SetValue(AvatarColorProperty, value);
        }

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public bool IsOnline
        {
            get => (bool)GetValue(IsOnlineProperty);
            set => SetValue(IsOnlineProperty, value);
        }

        public AvatarControl()
        {
            InitializeComponent();
        }
    }
}
