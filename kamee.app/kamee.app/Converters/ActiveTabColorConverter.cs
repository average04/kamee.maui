using System.Globalization;

namespace kamee.app.Converters
{
    public class ActiveTabColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var activeTab = value as string;
            var thisTab = parameter as string;
            return activeTab == thisTab
                ? Application.Current!.Resources["Mint"]
                : Application.Current!.Resources["TextSecondary"];
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
