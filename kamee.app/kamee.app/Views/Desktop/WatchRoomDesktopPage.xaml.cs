using System.Globalization;
using kamee.app.Services;
using kamee.app.ViewModels;

namespace kamee.app.Views.Desktop
{
    [QueryProperty(nameof(RoomId), "roomId")]
    public partial class WatchRoomDesktopPage : ContentPage
    {
        private string _roomId = string.Empty;
        private bool _playerReady;
        private bool _playerSourceSet;
        private string _lastBroadcastVideoId = string.Empty;

        public string RoomId
        {
            get => _roomId;
            set
            {
                _roomId = value;
                if (BindingContext is WatchRoomViewModel vm)
                    vm.RoomId = value;
            }
        }

        public WatchRoomDesktopPage(WatchRoomViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, false);
            if (BindingContext is not WatchRoomViewModel vm) return;

            vm.OnRemotePlay = pos =>
            {
                if (!_playerReady) return;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var p = pos.ToString(CultureInfo.InvariantCulture);
                        await playerWebView.EvaluateJavaScriptAsync(
                            $"window._kameeLastCmd=Date.now();var v=document.querySelector('video');if(v){{if({p}>=0)v.currentTime={p};v.play();}}");
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"RemotePlay JS failed: {ex.Message}"); }
                });
            };
            vm.OnRemotePause = pos =>
            {
                if (!_playerReady) return;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var p = pos.ToString(CultureInfo.InvariantCulture);
                        await playerWebView.EvaluateJavaScriptAsync(
                            $"window._kameeLastCmd=Date.now();var v=document.querySelector('video');if(v){{if({p}>=0)v.currentTime={p};v.pause();}}");
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"RemotePause JS failed: {ex.Message}"); }
                });
            };
            vm.OnRemoteSeek = pos =>
            {
                if (!_playerReady) return;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var p = pos.ToString(CultureInfo.InvariantCulture);
                        await playerWebView.EvaluateJavaScriptAsync(
                            $"window._kameeLastCmd=Date.now();var v=document.querySelector('video');if(v)v.currentTime={p};");
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"RemoteSeek JS failed: {ex.Message}"); }
                });
            };
            vm.OnRemoteVideoChanged = videoId =>
            {
                if (string.IsNullOrEmpty(videoId)) return;
                _playerReady = false;
                _playerSourceSet = false;
                playerWebView.Source = new UrlWebViewSource { Url = YoutubePlayerHtml.GetWatchUrl(videoId) };
                _playerSourceSet = true;
            };

            await vm.LoadAsync();

            if (!_playerSourceSet && !string.IsNullOrEmpty(vm.YoutubeVideoId))
            {
                _playerSourceSet = true;
                playerWebView.Source = new UrlWebViewSource { Url = YoutubePlayerHtml.GetWatchUrl(vm.YoutubeVideoId) };
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            _playerReady = false;
            _playerSourceSet = false;
            _lastBroadcastVideoId = string.Empty;
            if (BindingContext is WatchRoomViewModel vm)
                await vm.CleanupAsync();
        }

        private async void OnPlayerNavigated(object sender, WebNavigatedEventArgs e)
        {
            if (!e.Url.StartsWith("https://www.youtube.com/watch")) return;
            try
            {
                await Task.Delay(1500);
                await playerWebView.EvaluateJavaScriptAsync(YoutubePlayerHtml.GetBridgeJs());
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Bridge inject failed: {ex.Message}"); }
        }

        private void OnPlayerNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("kamee://")) return;
            e.Cancel = true;
            if (BindingContext is not WatchRoomViewModel vm) return;

            var position = ParsePosition(e.Url);
            var action = new Uri(e.Url).Host;

            switch (action)
            {
                case "ready":
                    _playerReady = true;
                    break;
                case "playing":
                    vm.IsPlaying = true;
                    vm.PlaybackPosition = position;
                    _ = vm.BroadcastPlayAsync(position).ContinueWith(
                        t => System.Diagnostics.Debug.WriteLine($"BroadcastPlay failed: {t.Exception?.GetBaseException().Message}"),
                        TaskContinuationOptions.OnlyOnFaulted);
                    break;
                case "paused":
                    vm.IsPlaying = false;
                    vm.PlaybackPosition = position;
                    _ = vm.BroadcastPauseAsync(position).ContinueWith(
                        t => System.Diagnostics.Debug.WriteLine($"BroadcastPause failed: {t.Exception?.GetBaseException().Message}"),
                        TaskContinuationOptions.OnlyOnFaulted);
                    break;
            }
        }

        private void OnHostGoTapped(object sender, TappedEventArgs e) => NavigateHost();
        private void OnHostUrlCompleted(object sender, EventArgs e) => NavigateHost();
        private void NavigateHost()
        {
            var url = hostUrlEntry.Text?.Trim() ?? string.Empty;
            if (!url.StartsWith("http")) url = "https://" + url;
            hostWebView.Source = new UrlWebViewSource { Url = url };
        }

        private void OnHostNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (BindingContext is not WatchRoomViewModel vm) return;
            if (!vm.IsHost) return;

            var videoId = YoutubePlayerHtml.ExtractVideoId(e.Url);
            if (string.IsNullOrEmpty(videoId)) return;
            if (videoId == _lastBroadcastVideoId) return;
            _lastBroadcastVideoId = videoId;

            _playerReady = false;
            _playerSourceSet = false;
            playerWebView.Source = new UrlWebViewSource { Url = YoutubePlayerHtml.GetWatchUrl(videoId) };
            _playerSourceSet = true;

            _ = vm.BroadcastVideoChangedAsync(videoId).ContinueWith(
                t => System.Diagnostics.Debug.WriteLine($"BroadcastVideoChanged failed: {t.Exception?.GetBaseException().Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private static double ParsePosition(string url)
        {
            var q = url.IndexOf('?');
            if (q < 0) return -1;
            foreach (var pair in url.Substring(q + 1).Split('&'))
            {
                var kv = pair.Split('=');
                if (kv.Length == 2 && kv[0] == "position" &&
                    double.TryParse(kv[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return d;
            }
            return -1;
        }
    }
}
