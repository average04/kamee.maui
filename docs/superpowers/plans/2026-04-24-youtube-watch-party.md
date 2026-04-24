# YouTube Watch Party Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Embed a synchronized YouTube player in the WatchRoom so all viewers play and pause together in real time.

**Architecture:** YouTube's iframe Player API runs inside a MAUI `WebView` loaded from an inline HTML string. JS→C# communication uses custom URL scheme interception (`kamee://playing`, `kamee://paused`) via `WebView.Navigating`. C#→JS uses `EvaluateJavaScriptAsync`. When a viewer triggers play/pause on their YouTube player, the page code-behind calls `WatchRoomViewModel.BroadcastPlayAsync/PauseAsync`, which publishes to Supabase Realtime via the existing `SyncService`. Remote viewers receive the event and the page executes JS to control their player.

**Tech Stack:** .NET 10 MAUI, YouTube iframe API, MAUI `WebView`, Supabase Realtime (already wired in `SyncService`)

---

## File Map

| File | Change |
|------|--------|
| `kamee.app/kamee.app/Services/RoomService.cs` | Add `videoUrl` parameter to `CreateRoomAsync` |
| `kamee.app/kamee.app/ViewModels/CreateRoomViewModel.cs` | Add `VideoUrl` property; pass it to `CreateRoomAsync` |
| `kamee.app/kamee.app/Views/Mobile/CreateRoomPage.xaml` | Add video URL `Entry` field |
| `kamee.app/kamee.app/Services/YoutubePlayerHtml.cs` | New — static HTML template with YouTube iframe API and JS bridge functions |
| `kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs` | Add `VideoUrl`, `YoutubeVideoId`, remote action callbacks, public broadcast methods |
| `kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml` | Replace gradient placeholder with `WebView` |
| `kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml.cs` | Wire JS bridge: Navigating handler + remote sync JS calls |
| `kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml` | Replace gradient placeholder with `WebView` |
| `kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml.cs` | Wire JS bridge: same pattern as mobile |

No new migrations needed — `rooms.video_url` and `rooms.streaming_platform` already exist in `supabase/migrations/20260423000001_initial_schema.sql`.

---

### Task 1: Add VideoUrl to the Create Room flow

Wire the `video_url` column that already exists in the DB into the create-room UI and service layer.

**Files:**
- Modify: `kamee.app/kamee.app/Services/RoomService.cs`
- Modify: `kamee.app/kamee.app/ViewModels/CreateRoomViewModel.cs`
- Modify: `kamee.app/kamee.app/Views/Mobile/CreateRoomPage.xaml`

- [ ] **Step 1: Add `videoUrl` parameter to `RoomService.CreateRoomAsync`**

Read `kamee.app/kamee.app/Services/RoomService.cs`. Replace the `CreateRoomAsync` method:

```csharp
public async Task<Room?> CreateRoomAsync(string name, string platform, string? videoUrl, bool isPrivate)
{
    var userId = _authService.GetCurrentUserId()
        ?? throw new InvalidOperationException("User not logged in");

    var room = new Room
    {
        Name = name,
        HostId = userId,
        StreamingPlatform = platform,
        VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl.Trim(),
        IsPrivate = isPrivate,
        IsLive = true,
        CreatedAt = DateTime.UtcNow
    };

    var response = await _supabase.Client.From<Room>().Insert(room);
    return response.Model;
}
```

- [ ] **Step 2: Add `VideoUrl` property to `CreateRoomViewModel` and pass it on Create**

Read `kamee.app/kamee.app/ViewModels/CreateRoomViewModel.cs`. Add the observable property after `_isPrivate`:

```csharp
[ObservableProperty]
private string _videoUrl = string.Empty;
```

Update the `Create()` command body — change the `CreateRoomAsync` call from:
```csharp
var room = await _roomService.CreateRoomAsync(RoomName, SelectedPlatform, IsPrivate);
```
To:
```csharp
var room = await _roomService.CreateRoomAsync(RoomName, SelectedPlatform, VideoUrl, IsPrivate);
```

- [ ] **Step 3: Add URL input field to `CreateRoomPage.xaml`**

Read `kamee.app/kamee.app/Views/Mobile/CreateRoomPage.xaml`. Add the following block directly **after** the closing `</CollectionView>` of the platform picker (before the privacy toggle `<Border>`):

```xml
<!-- Video URL -->
<Label Text="Video URL" TextColor="{StaticResource TextSecondary}"
       FontSize="12" Margin="4,0,0,6" />
<Border Style="{StaticResource InputBorder}" Margin="0,0,0,24">
    <Entry Text="{Binding VideoUrl}"
           Placeholder="YouTube URL  (e.g. youtube.com/watch?v=...)"
           Style="{StaticResource KameeEntry}"
           Keyboard="Url" />
</Border>
```

- [ ] **Step 4: Build**

```
cd "d:\Projects\kamee.maui\kamee.app" && dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore 2>&1 | tail -4
```

Expected: `0 Error(s)`

---

### Task 2: Add `VideoUrl` and sync callbacks to `WatchRoomViewModel`

Expose `VideoUrl` from the loaded room, a computed `YoutubeVideoId`, public broadcast methods callable from the page, and `Action` callbacks the page wires up to execute JS when remote sync events arrive.

**Files:**
- Modify: `kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs`

- [ ] **Step 1: Add `VideoUrl` observable property and `YoutubeVideoId` computed property**

Read `kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs`. Add after the existing `[ObservableProperty] private ObservableCollection<User> _viewers`:

```csharp
[ObservableProperty]
private string _videoUrl = string.Empty;

public string YoutubeVideoId => ExtractYoutubeId(_videoUrl);

private static string ExtractYoutubeId(string url)
{
    if (string.IsNullOrWhiteSpace(url)) return string.Empty;
    var match = System.Text.RegularExpressions.Regex.Match(
        url,
        @"(?:youtube\.com/watch\?.*v=|youtu\.be/|youtube\.com/embed/)([a-zA-Z0-9_-]{11})");
    return match.Success ? match.Groups[1].Value : string.Empty;
}

// Set by the page after LoadAsync so remote sync events execute JS on the player.
public Action<double>? OnRemotePlay { get; set; }
public Action<double>? OnRemotePause { get; set; }
public Action<double>? OnRemoteSeek { get; set; }
```

- [ ] **Step 2: Add public `BroadcastPlayAsync` and `BroadcastPauseAsync` methods**

Add these two public methods before `CleanupAsync`:

```csharp
public Task BroadcastPlayAsync(double position) =>
    _syncService.BroadcastPlayAsync(RoomId, position);

public Task BroadcastPauseAsync(double position) =>
    _syncService.BroadcastPauseAsync(RoomId, position);
```

- [ ] **Step 3: Set `VideoUrl` in `LoadAsync` and wire remote callbacks**

In `LoadAsync`, after the line `RoomName = room?.Name ?? string.Empty;` add:
```csharp
VideoUrl = room?.VideoUrl ?? string.Empty;
```

Replace the existing `_syncService.SubscribeToSyncAsync` call with:

```csharp
await _syncService.SubscribeToSyncAsync(
    RoomId,
    position => { PlaybackPosition = position; IsPlaying = true;  OnRemotePlay?.Invoke(position); },
    position => { PlaybackPosition = position; IsPlaying = false; OnRemotePause?.Invoke(position); },
    position => { PlaybackPosition = position; OnRemoteSeek?.Invoke(position); });
```

- [ ] **Step 4: Build**

```
cd "d:\Projects\kamee.maui\kamee.app" && dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore 2>&1 | tail -4
```

Expected: `0 Error(s)`

---

### Task 3: Create `YoutubePlayerHtml` helper

A static class that returns the complete HTML page string with the YouTube iframe API wired up and a JS bridge for C# communication.

**Files:**
- Create: `kamee.app/kamee.app/Services/YoutubePlayerHtml.cs`

- [ ] **Step 1: Create the file**

Create `kamee.app/kamee.app/Services/YoutubePlayerHtml.cs` with this exact content:

```csharp
namespace kamee.app.Services
{
    public static class YoutubePlayerHtml
    {
        public static string GetHtml(string videoId) => $"""
            <!DOCTYPE html>
            <html>
            <head>
              <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no">
              <style>
                * {{ margin: 0; padding: 0; box-sizing: border-box; }}
                body {{ background: #000; width: 100vw; height: 100vh; overflow: hidden; }}
                #player {{ width: 100%; height: 100%; }}
              </style>
            </head>
            <body>
              <div id="player"></div>
              <script>
                var tag = document.createElement('script');
                tag.src = 'https://www.youtube.com/iframe_api';
                document.head.appendChild(tag);

                var player;

                function onYouTubeIframeAPIReady() {{
                  player = new YT.Player('player', {{
                    videoId: '{videoId}',
                    playerVars: {{ autoplay: 0, controls: 1, rel: 0, modestbranding: 1, playsinline: 1 }},
                    events: {{
                      onReady: function() {{ window.location.href = 'kamee://ready'; }},
                      onStateChange: function(e) {{
                        if (!player) return;
                        var pos = player.getCurrentTime().toFixed(3);
                        if (e.data === 1) window.location.href = 'kamee://playing?position=' + pos;
                        else if (e.data === 2) window.location.href = 'kamee://paused?position=' + pos;
                      }}
                    }}
                  }});
                }}

                function playVideo(pos) {{
                  if (!player) return;
                  if (pos >= 0) player.seekTo(pos, true);
                  player.playVideo();
                }}

                function pauseVideo(pos) {{
                  if (!player) return;
                  if (pos >= 0) player.seekTo(pos, true);
                  player.pauseVideo();
                }}

                function seekVideo(pos) {{
                  if (player) player.seekTo(pos, true);
                }}
              </script>
            </body>
            </html>
            """;
    }
}
```

- [ ] **Step 2: Build**

```
cd "d:\Projects\kamee.maui\kamee.app" && dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore 2>&1 | tail -4
```

Expected: `0 Error(s)`

---

### Task 4: Wire WebView into mobile WatchRoomPage

Replace the gradient placeholder in Row 0 with a `WebView`. The page code-behind loads the YouTube HTML after `LoadAsync` and handles the `kamee://` URL bridge in both directions.

**Files:**
- Modify: `kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml`
- Modify: `kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml.cs`

- [ ] **Step 1: Replace the video placeholder in `WatchRoomPage.xaml`**

Read `kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml`. Replace the entire first `<Grid>` (Row 0, the one with `HeightRequest="130"` and the gradient background, play button, and progress bar) with:

```xml
<!-- Video Player -->
<Grid Grid.Row="0" HeightRequest="220">
    <WebView x:Name="playerWebView"
             BackgroundColor="Black"
             Navigating="OnPlayerNavigating" />
    <VerticalStackLayout VerticalOptions="End" HorizontalOptions="Start"
                         Margin="12,0,0,8" Spacing="0"
                         InputTransparent="True">
        <Label Text="{Binding RoomName}"
               TextColor="White" FontSize="11" FontAttributes="Bold" />
    </VerticalStackLayout>
</Grid>
```

Also change the outer Grid's first row definition from `Auto` to `Auto` (no change needed — row 0 is Auto and the WebView with HeightRequest="220" will size it).

- [ ] **Step 2: Replace `WatchRoomPage.xaml.cs` with JS bridge wiring**

Write the full content of `kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml.cs`:

```csharp
using System.Globalization;
using kamee.app.Services;
using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    [QueryProperty(nameof(RoomId), "roomId")]
    public partial class WatchRoomPage : ContentPage
    {
        private string _roomId = string.Empty;
        private bool _playerReady;

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

        public WatchRoomPage(WatchRoomViewModel viewModel)
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
                    await playerWebView.EvaluateJavaScriptAsync(
                        $"playVideo({pos.ToString(CultureInfo.InvariantCulture)})"));
            };
            vm.OnRemotePause = pos =>
            {
                if (!_playerReady) return;
                MainThread.BeginInvokeOnMainThread(async () =>
                    await playerWebView.EvaluateJavaScriptAsync(
                        $"pauseVideo({pos.ToString(CultureInfo.InvariantCulture)})"));
            };
            vm.OnRemoteSeek = pos =>
            {
                if (!_playerReady) return;
                MainThread.BeginInvokeOnMainThread(async () =>
                    await playerWebView.EvaluateJavaScriptAsync(
                        $"seekVideo({pos.ToString(CultureInfo.InvariantCulture)})"));
            };

            await vm.LoadAsync();

            if (!string.IsNullOrEmpty(vm.YoutubeVideoId))
                playerWebView.Source = new HtmlWebViewSource
                    { Html = YoutubePlayerHtml.GetHtml(vm.YoutubeVideoId) };
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            _playerReady = false;
            if (BindingContext is WatchRoomViewModel vm)
                await vm.CleanupAsync();
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
                    _ = vm.BroadcastPlayAsync(position);
                    break;
                case "paused":
                    vm.IsPlaying = false;
                    vm.PlaybackPosition = position;
                    _ = vm.BroadcastPauseAsync(position);
                    break;
            }
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
```

- [ ] **Step 3: Build**

```
cd "d:\Projects\kamee.maui\kamee.app" && dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore 2>&1 | tail -4
```

Expected: `0 Error(s)`

---

### Task 5: Wire WebView into desktop WatchRoomDesktopPage

Same JS bridge pattern applied to the desktop layout. The video area is in column 1, row 1 of the desktop grid.

**Files:**
- Modify: `kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml`
- Modify: `kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml.cs`

- [ ] **Step 1: Replace the video placeholder in `WatchRoomDesktopPage.xaml`**

Read `kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml`. In column 1's grid (the one with `RowDefinitions="Auto,*,Auto"`), find the `<Grid Grid.Row="1" Margin="20,0">` that contains the `Border` with `RadialGradientBrush` and the play button. Replace it entirely with:

```xml
<!-- Video player -->
<Grid Grid.Row="1" Margin="20,0">
    <Border StrokeShape="RoundRectangle 12" Stroke="Transparent" BackgroundColor="Black">
        <WebView x:Name="playerWebView"
                 BackgroundColor="Black"
                 Navigating="OnPlayerNavigating" />
    </Border>
</Grid>
```

Also remove the controls bar row (Grid.Row="2") that contains the standalone `▶` play label and the static progress bar — those are now provided by YouTube's built-in controls. Replace that entire `<Grid Grid.Row="2" ...>` with a simple time display, or remove it entirely. Remove it:

Find and delete this block:
```xml
<!-- Controls bar -->
<Grid Grid.Row="2" Padding="20,12" ColumnDefinitions="Auto,*,Auto">
    ...
</Grid>
```

Update `RowDefinitions` on the column-1 grid from `"Auto,*,Auto"` to `"Auto,*"`.

- [ ] **Step 2: Replace `WatchRoomDesktopPage.xaml.cs` with JS bridge wiring**

Write the full content of `kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml.cs`:

```csharp
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
                    await playerWebView.EvaluateJavaScriptAsync(
                        $"playVideo({pos.ToString(CultureInfo.InvariantCulture)})"));
            };
            vm.OnRemotePause = pos =>
            {
                if (!_playerReady) return;
                MainThread.BeginInvokeOnMainThread(async () =>
                    await playerWebView.EvaluateJavaScriptAsync(
                        $"pauseVideo({pos.ToString(CultureInfo.InvariantCulture)})"));
            };
            vm.OnRemoteSeek = pos =>
            {
                if (!_playerReady) return;
                MainThread.BeginInvokeOnMainThread(async () =>
                    await playerWebView.EvaluateJavaScriptAsync(
                        $"seekVideo({pos.ToString(CultureInfo.InvariantCulture)})"));
            };

            await vm.LoadAsync();

            if (!string.IsNullOrEmpty(vm.YoutubeVideoId))
                playerWebView.Source = new HtmlWebViewSource
                    { Html = YoutubePlayerHtml.GetHtml(vm.YoutubeVideoId) };
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            _playerReady = false;
            if (BindingContext is WatchRoomViewModel vm)
                await vm.CleanupAsync();
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
                    _ = vm.BroadcastPlayAsync(position);
                    break;
                case "paused":
                    vm.IsPlaying = false;
                    vm.PlaybackPosition = position;
                    _ = vm.BroadcastPauseAsync(position);
                    break;
            }
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
```

- [ ] **Step 3: Build**

```
cd "d:\Projects\kamee.maui\kamee.app" && dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore 2>&1 | tail -4
```

Expected: `0 Error(s)`

---

## Self-Review

**Spec coverage:**
- ✅ YouTube URL input in Create Room (Task 1)
- ✅ YouTube video ID extraction from URL (Task 2)
- ✅ YouTube iframe loads in WebView (Tasks 3, 4, 5)
- ✅ Play/pause events from local player broadcast via SyncService (Tasks 4, 5 — Navigating handler)
- ✅ Remote play/pause events execute JS on the local player (Tasks 2, 4, 5 — OnRemotePlay/Pause callbacks)
- ✅ `_playerReady` guard prevents JS calls before iframe is ready (Tasks 4, 5)
- ⏭ Seeking sync — YouTube doesn't expose a native seek event; deferred

**Placeholder scan:** No TBDs. All code blocks are complete.

**Type consistency:**
- `BroadcastPlayAsync(double)` / `BroadcastPauseAsync(double)` defined in Task 2, called in Tasks 4 and 5 — match.
- `OnRemotePlay`, `OnRemotePause`, `OnRemoteSeek` defined as `Action<double>?` in Task 2, assigned in Tasks 4 and 5 — match.
- `YoutubePlayerHtml.GetHtml(string)` defined in Task 3, called in Tasks 4 and 5 — match.
- `ParsePosition` returns `-1` on failure; `playVideo(-1)` / `pauseVideo(-1)` — in JS: `if (pos >= 0) player.seekTo(pos, true)` handles -1 correctly (skips seek, just plays/pauses) — correct.
