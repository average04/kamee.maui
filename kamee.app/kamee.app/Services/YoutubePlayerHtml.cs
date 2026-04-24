namespace kamee.app.Services
{
    public static class YoutubePlayerHtml
    {
        private static readonly System.Text.RegularExpressions.Regex _videoIdRegex = new(
            @"(?:youtube\.com/watch\?.*v=|youtu\.be/|youtube\.com/embed/|youtube\.com/shorts/)([a-zA-Z0-9_-]{11})",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        public static string? ExtractVideoId(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var m = _videoIdRegex.Match(url);
            return m.Success ? m.Groups[1].Value : null;
        }

        public static string GetWatchUrl(string videoId)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(videoId, @"^[a-zA-Z0-9_-]{11}$"))
                throw new ArgumentException("Invalid YouTube video ID.", nameof(videoId));
            return $"https://www.youtube.com/watch?v={videoId}";
        }

        // Injected into the full youtube.com/watch page after it loads.
        // Waits for the <video> element, attaches play/pause listeners that
        // fire kamee:// scheme navigations intercepted by the MAUI WebView.
        // _kameeLastCmd suppresses echo events triggered by remote-control JS.
        public static string GetBridgeJs() => """
            (function() {
              if (window._kameeReady) return;
              window._kameeReady = true;
              window._kameeLastCmd = 0;
              function tryAttach() {
                var v = document.querySelector('video');
                if (!v) { setTimeout(tryAttach, 500); return; }
                v.addEventListener('play', function() {
                  if (Date.now() - window._kameeLastCmd < 1000) return;
                  window.location.href = 'kamee://playing?position=' + v.currentTime.toFixed(3);
                });
                v.addEventListener('pause', function() {
                  if (Date.now() - window._kameeLastCmd < 1000) return;
                  if (v.ended) return;
                  window.location.href = 'kamee://paused?position=' + v.currentTime.toFixed(3);
                });
                window.location.href = 'kamee://ready';
              }
              tryAttach();
            })();
            """;
    }
}
