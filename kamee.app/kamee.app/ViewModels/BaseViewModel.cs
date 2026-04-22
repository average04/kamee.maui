using CommunityToolkit.Mvvm.ComponentModel;

namespace kamee.app.ViewModels
{
    public class BaseViewModel : ObservableObject
    {
        private bool isBusy;
        public bool IsBusy
        {
            get => isBusy;
            set => SetProperty(ref isBusy, value);
        }

        private string title = string.Empty;
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }
    }
}
