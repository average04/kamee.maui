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

        // Host sync polling
        private System.Timers.Timer? _pollTimer;
        private bool _pollLastPaused = true;

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
                            $"(function(){{var v=document.querySelector('video');if(v){{if({p}>=0)v.currentTime={p};v.play();}}}})()");
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
                            $"(function(){{var v=document.querySelector('video');if(v){{if({p}>=0)v.currentTime={p};v.pause();}}}})()");
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
                            $"(function(){{var v=document.querySelector('video');if(v)v.currentTime={p};}})()");
                    }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"RemoteSeek JS failed: {ex.Message}"); }
                });
            };
            vm.OnRemoteVideoChanged = videoId =>
            {
                if (string.IsNullOrEmpty(videoId)) return;
                StopPolling();
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
            StopPolling();
            _playerReady = false;
            _playerSourceSet = false;
            _lastBroadcastVideoId = string.Empty;
            if (BindingContext is WatchRoomViewModel vm)
                await vm.CleanupAsync();
        }

        // Fires when the YouTube watch page finishes loading.
        // Waits for the player to render, then marks ready and (for host) starts polling.
        private async void OnPlayerNavigated(object sender, WebNavigatedEventArgs e)
        {
            if (!e.Url.StartsWith("https://www.youtube.com/watch")) return;
            await Task.Delay(2000);
            _playerReady = true;
            if (BindingContext is WatchRoomViewModel vm && vm.IsHost)
                StartHostPolling();
        }

        // Safety net: cancel any stray kamee:// navigations from the player.
        private void OnPlayerNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("kamee://"))
                e.Cancel = true;
        }

        // ── Host sync polling ────────────────────────────────────────────────

        private void StartHostPolling()
        {
            StopPolling();
            _pollLastPaused = true;
            _pollTimer = new System.Timers.Timer(1000) { AutoReset = true };
            _pollTimer.Elapsed += OnPollTick;
            _pollTimer.Start();
        }

        private void StopPolling()
        {
            _pollTimer?.Stop();
            _pollTimer?.Dispose();
            _pollTimer = null;
        }

        private async void OnPollTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (BindingContext is not WatchRoomViewModel vm || !vm.IsHost) return;
            try
            {
                // Returns "1|<time>" if paused, "0|<time>" if playing, null if no video element yet.
                var result = await MainThread.InvokeOnMainThreadAsync(() =>
                    playerWebView.EvaluateJavaScriptAsync(
                        "(function(){var v=document.querySelector('video');if(!v||isNaN(v.currentTime))return null;return(v.paused?'1':'0')+'|'+v.currentTime.toFixed(2);})()"));

                if (result == null || result == "null") return;
                result = result.Trim('"');
                var parts = result.Split('|');
                if (parts.Length != 2) return;
                bool paused = parts[0] == "1";
                if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double t)) return;

                if (paused == _pollLastPaused) return;
                _pollLastPaused = paused;

                if (!paused)
                    _ = vm.BroadcastPlayAsync(t);
                else
                    _ = vm.BroadcastPauseAsync(t);
            }
            catch { }
        }

        // ── Host browser ─────────────────────────────────────────────────────

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

            StopPolling();
            _playerReady = false;
            _playerSourceSet = false;
            playerWebView.Source = new UrlWebViewSource { Url = YoutubePlayerHtml.GetWatchUrl(videoId) };
            _playerSourceSet = true;

            _ = vm.BroadcastVideoChangedAsync(videoId).ContinueWith(
                t => System.Diagnostics.Debug.WriteLine($"BroadcastVideoChanged failed: {t.Exception?.GetBaseException().Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
