namespace kamee.app.Controls
{
    public partial class ChatBubble : ContentView
    {
        public static readonly BindableProperty MessageContentProperty =
            BindableProperty.Create(nameof(MessageContent), typeof(string), typeof(ChatBubble), string.Empty);

        public static readonly BindableProperty SenderNameProperty =
            BindableProperty.Create(nameof(SenderName), typeof(string), typeof(ChatBubble), string.Empty);

        public static readonly BindableProperty AvatarInitialsProperty =
            BindableProperty.Create(nameof(AvatarInitials), typeof(string), typeof(ChatBubble), "??");

        public static readonly BindableProperty IsFromCurrentUserProperty =
            BindableProperty.Create(nameof(IsFromCurrentUser), typeof(bool), typeof(ChatBubble), false);

        public string MessageContent
        {
            get => (string)GetValue(MessageContentProperty);
            set => SetValue(MessageContentProperty, value);
        }

        public string SenderName
        {
            get => (string)GetValue(SenderNameProperty);
            set => SetValue(SenderNameProperty, value);
        }

        public string AvatarInitials
        {
            get => (string)GetValue(AvatarInitialsProperty);
            set => SetValue(AvatarInitialsProperty, value);
        }

        public bool IsFromCurrentUser
        {
            get => (bool)GetValue(IsFromCurrentUserProperty);
            set => SetValue(IsFromCurrentUserProperty, value);
        }

        public ChatBubble()
        {
            InitializeComponent();
        }
    }
}
