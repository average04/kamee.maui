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
    }
}
