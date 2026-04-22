# Missing Features Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire up the six functional gaps left after the initial scaffold: room-name display, message sender names, watch history on profile, profile stats, RoomCard tap navigation, and tab active-state indicator.

**Architecture:** All fixes are local — no new services or screens needed. Data enrichment (usernames, room details) happens in the existing `ChatService` and `RoomService`. QueryProperty routing is moved to the page level (MAUI-safe pattern). UI binding fixes are XAML-only.

**Tech Stack:** .NET 10 MAUI, Supabase postgrest-csharp 3.5.1, CommunityToolkit.Mvvm 8.x

---

## File Map

| File | Change |
|------|--------|
| `kamee.app/Services/RoomService.cs` | Add `GetRoomAsync`, `GetWatchHistoryAsync`, `GetProfileStatsAsync` |
| `kamee.app/Services/ChatService.cs` | Enrich messages with usernames from profiles |
| `kamee.app/ViewModels/WatchRoomViewModel.cs` | Remove `[QueryProperty]` (move to page) |
| `kamee.app/Views/Mobile/WatchRoomPage.xaml.cs` | Add `[QueryProperty]` + forward RoomId to VM |
| `kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml.cs` | Add `[QueryProperty]` + forward RoomId to VM |
| `kamee.app/ViewModels/ProfileViewModel.cs` | Load watch history + stats |
| `kamee.app/Views/Mobile/ProfilePage.xaml` | Bind stats labels |
| `kamee.app/Views/Mobile/HomePage.xaml` | Wire RoomCard TapCommand; fix tab indicator |
| `kamee.app/Views/Desktop/HomeDesktopPage.xaml` | Wire RoomCard TapCommand |

---

### Task 1: Fix QueryProperty routing for WatchRoom

Shell navigation with `GoToAsync("watchroom?roomId=xxx")` requires `[QueryProperty]` on the **page**, not the ViewModel. Currently it's only on the ViewModel so `RoomId` is always empty and `LoadAsync` loads nothing.

**Files:**
- Modify: `kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs`
- Modify: `kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml.cs`
- Modify: `kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml.cs`

- [ ] **Step 1: Remove `[QueryProperty]` from WatchRoomViewModel**

In `kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs`, remove the attribute:

```csharp
// Before:
[QueryProperty(nameof(RoomId), "roomId")]
public partial class WatchRoomViewModel : BaseViewModel

// After:
public partial class WatchRoomViewModel : BaseViewModel
```

- [ ] **Step 2: Add QueryProperty to WatchRoomPage**

Replace the full content of `kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml.cs`:

```csharp
using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    [QueryProperty(nameof(RoomId), "roomId")]
    public partial class WatchRoomPage : ContentPage
    {
        private string _roomId = string.Empty;
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
            if (BindingContext is WatchRoomViewModel vm)
                await vm.LoadAsync();
        }
    }
}
```

- [ ] **Step 3: Add QueryProperty to WatchRoomDesktopPage**

Replace the full content of `kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml.cs`:

```csharp
using kamee.app.ViewModels;

namespace kamee.app.Views.Desktop
{
    [QueryProperty(nameof(RoomId), "roomId")]
    public partial class WatchRoomDesktopPage : ContentPage
    {
        private string _roomId = string.Empty;
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
            if (BindingContext is WatchRoomViewModel vm)
                await vm.LoadAsync();
        }
    }
}
```

- [ ] **Step 4: Build and verify no compile errors**

```
cd kamee.app
dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs
git add kamee.app/kamee.app/Views/Mobile/WatchRoomPage.xaml.cs
git add kamee.app/kamee.app/Views/Desktop/WatchRoomDesktopPage.xaml.cs
git commit -m "fix: move QueryProperty to page level for reliable roomId routing"
```

---

### Task 2: Load room name in WatchRoomViewModel

`RoomName` is bound in both watch room XAML files but `LoadAsync` never fetches the room record, so the title is always blank.

**Files:**
- Modify: `kamee.app/kamee.app/Services/RoomService.cs`
- Modify: `kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs`

- [ ] **Step 1: Add `GetRoomAsync` to RoomService**

Add this method to `kamee.app/kamee.app/Services/RoomService.cs` after `GetLiveRoomsAsync`:

```csharp
public async Task<Room?> GetRoomAsync(string roomId)
{
    var response = await _supabase.Client
        .From<Room>()
        .Filter("id", Postgrest.Constants.Operator.Equals, roomId)
        .Single();
    return response;
}
```

- [ ] **Step 2: Inject RoomService into WatchRoomViewModel and fetch room on load**

In `kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs`, update `LoadAsync`:

```csharp
public async Task LoadAsync()
{
    if (string.IsNullOrEmpty(RoomId)) return;
    IsBusy = true;

    try
    {
        var room = await _roomService.GetRoomAsync(RoomId);
        RoomName = room?.Name ?? string.Empty;

        var messages = await _chatService.GetMessagesAsync(RoomId);
        Messages = new ObservableCollection<Message>(messages);

        await _chatService.SubscribeToRoomAsync(RoomId, OnMessageReceived);

        await _syncService.SubscribeToSyncAsync(
            RoomId,
            position => { PlaybackPosition = position; IsPlaying = true; },
            position => { PlaybackPosition = position; IsPlaying = false; },
            position => { PlaybackPosition = position; });
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"WatchRoom load error: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
```

(`_roomService` is already injected in the constructor — no constructor change needed.)

- [ ] **Step 3: Build**

```
dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add kamee.app/kamee.app/Services/RoomService.cs
git add kamee.app/kamee.app/ViewModels/WatchRoomViewModel.cs
git commit -m "feat: load room name in watch room header"
```

---

### Task 3: Populate message sender usernames

`ChatBubble` binds `SenderName` and `AvatarInitials` from `Message`, but those fields are never populated — the query only returns the raw DB columns (`user_id`, `content`, etc.). Fix by batch-fetching profiles for all message authors.

**Files:**
- Modify: `kamee.app/kamee.app/Services/ChatService.cs`

- [ ] **Step 1: Add profile batch-fetch and enrich messages**

Replace `GetMessagesAsync` in `kamee.app/kamee.app/Services/ChatService.cs`:

```csharp
public async Task<List<Message>> GetMessagesAsync(string roomId)
{
    var response = await _supabase.Client
        .From<Message>()
        .Filter("room_id", Postgrest.Constants.Operator.Equals, roomId)
        .Order("sent_at", Postgrest.Constants.Ordering.Ascending)
        .Get();

    var messages = response.Models;
    var currentUserId = _authService.GetCurrentUserId();

    // Batch-fetch profiles for all unique senders
    var userIds = messages.Select(m => m.UserId).Distinct().ToList();
    var profiles = await FetchProfilesAsync(userIds);

    foreach (var msg in messages)
    {
        msg.IsFromCurrentUser = msg.UserId == currentUserId;
        if (profiles.TryGetValue(msg.UserId, out var profile))
        {
            msg.Username = profile.Username;
            msg.AvatarInitials = profile.AvatarInitials;
        }
    }

    return messages;
}

private async Task<Dictionary<string, User>> FetchProfilesAsync(List<string> userIds)
{
    if (userIds.Count == 0) return new Dictionary<string, User>();

    // postgrest-csharp: filter with In operator using a CSV string
    var csv = string.Join(",", userIds);
    var response = await _supabase.Client
        .From<User>()
        .Filter("id", Postgrest.Constants.Operator.In, $"({csv})")
        .Get();

    return response.Models.ToDictionary(u => u.Id);
}
```

- [ ] **Step 2: Enrich incoming realtime messages with sender username**

The `SubscribeToRoomAsync` handler receives messages without usernames. Update the handler so it fetches the sender's profile for new incoming messages.

In `SubscribeToRoomAsync` in `ChatService.cs`, update the handler:

```csharp
_channel.AddPostgresChangeHandler(PostgresChangesOptions.ListenType.Inserts, async (_, change) =>
{
    if (change.Model<Message>() is not Message msg) return;

    msg.IsFromCurrentUser = msg.UserId == _authService.GetCurrentUserId();

    // Fetch sender profile
    try
    {
        var profileResponse = await _supabase.Client
            .From<User>()
            .Filter("id", Postgrest.Constants.Operator.Equals, msg.UserId)
            .Single();

        if (profileResponse != null)
        {
            msg.Username = profileResponse.Username;
            msg.AvatarInitials = profileResponse.AvatarInitials;
        }
    }
    catch { /* username stays empty on failure */ }

    onMessage(msg);
});
```

Note: The handler lambda must become `async` — change the signature from `(_, change) =>` to `async (_, change) =>`.

- [ ] **Step 3: Build**

```
dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add kamee.app/kamee.app/Services/ChatService.cs
git commit -m "feat: populate message sender username and avatar initials"
```

---

### Task 4: Wire RoomCard tap to JoinRoom

`RoomCard` has a `TapCommand` bindable property but it's never bound in the DataTemplate in `HomePage.xaml` or `HomeDesktopPage.xaml`. Tapping a card does nothing.

**Files:**
- Modify: `kamee.app/kamee.app/Views/Mobile/HomePage.xaml`
- Modify: `kamee.app/kamee.app/Views/Desktop/HomeDesktopPage.xaml`

- [ ] **Step 1: Bind TapCommand in mobile HomePage DataTemplate**

In `kamee.app/kamee.app/Views/Mobile/HomePage.xaml`, find the RoomCard DataTemplate and add the TapCommand binding. The DataTemplate's data type is `models:Room`, so we need `RelativeSource` to reach `HomeViewModel.JoinRoomCommand`.

Replace:
```xml
<DataTemplate x:DataType="models:Room">
    <controls:RoomCard Room="{Binding}" />
</DataTemplate>
```

With:
```xml
<DataTemplate x:DataType="models:Room">
    <controls:RoomCard
        Room="{Binding}"
        TapCommand="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.JoinRoomCommand}" />
</DataTemplate>
```

- [ ] **Step 2: Bind TapCommand in desktop HomeDesktopPage DataTemplate**

In `kamee.app/kamee.app/Views/Desktop/HomeDesktopPage.xaml`, find the RoomCard DataTemplate (inside the CollectionView in the main content area) and apply the same fix:

Replace:
```xml
<DataTemplate x:DataType="models:Room">
    <controls:RoomCard Room="{Binding}" />
</DataTemplate>
```

With:
```xml
<DataTemplate x:DataType="models:Room">
    <controls:RoomCard
        Room="{Binding}"
        TapCommand="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}}, Path=BindingContext.JoinRoomCommand}" />
</DataTemplate>
```

- [ ] **Step 3: Build**

```
dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add kamee.app/kamee.app/Views/Mobile/HomePage.xaml
git add kamee.app/kamee.app/Views/Desktop/HomeDesktopPage.xaml
git commit -m "fix: wire RoomCard tap to JoinRoomCommand"
```

---

### Task 5: Fix tab active-state indicator

The "For you / Friends / Trending" tabs in `HomePage.xaml` use `MultiBinding` with a null converter — the text color never changes. Replace with a simple `IValueConverter` that compares the tab label to `ActiveTab`.

**Files:**
- Create: `kamee.app/kamee.app/Converters/ActiveTabColorConverter.cs`
- Modify: `kamee.app/kamee.app/Views/Mobile/HomePage.xaml`

- [ ] **Step 1: Create the converter**

Create `kamee.app/kamee.app/Converters/ActiveTabColorConverter.cs`:

```csharp
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
```

- [ ] **Step 2: Register converter in AppStyles.xaml**

In `kamee.app/kamee.app/Resources/Styles/AppStyles.xaml`, add to the `ResourceDictionary`:

```xml
xmlns:converters="clr-namespace:kamee.app.Converters"
```

Add to the existing `xmlns` declarations at the top, then inside the dictionary:

```xml
<converters:ActiveTabColorConverter x:Key="ActiveTabColorConverter" />
```

- [ ] **Step 3: Fix tab labels in HomePage.xaml**

In `kamee.app/kamee.app/Views/Mobile/HomePage.xaml`, replace the entire `<!-- Tab row -->` section:

```xml
<!-- Tab row -->
<HorizontalStackLayout Padding="20,0" Spacing="24" Margin="0,0,0,20">
    <Label Text="For you" FontSize="14" FontAttributes="Bold"
           TextColor="{Binding ActiveTab, Converter={StaticResource ActiveTabColorConverter}, ConverterParameter='For you'}">
        <Label.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding SetActiveTabCommand}"
                                  CommandParameter="For you" />
        </Label.GestureRecognizers>
    </Label>
    <Label Text="Friends" FontSize="14"
           TextColor="{Binding ActiveTab, Converter={StaticResource ActiveTabColorConverter}, ConverterParameter='Friends'}">
        <Label.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding SetActiveTabCommand}"
                                  CommandParameter="Friends" />
        </Label.GestureRecognizers>
    </Label>
    <Label Text="Trending" FontSize="14"
           TextColor="{Binding ActiveTab, Converter={StaticResource ActiveTabColorConverter}, ConverterParameter='Trending'}">
        <Label.GestureRecognizers>
            <TapGestureRecognizer Command="{Binding SetActiveTabCommand}"
                                  CommandParameter="Trending" />
        </Label.GestureRecognizers>
    </Label>
</HorizontalStackLayout>
```

- [ ] **Step 4: Build**

```
dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add kamee.app/kamee.app/Converters/ActiveTabColorConverter.cs
git add kamee.app/kamee.app/Resources/Styles/AppStyles.xaml
git add kamee.app/kamee.app/Views/Mobile/HomePage.xaml
git commit -m "fix: implement tab active-state color using value converter"
```

---

### Task 6: Load watch history and profile stats

`ProfileViewModel.LoadAsync` only fetches `CurrentUser`. The watch history `CollectionView` is always empty, and the Rooms/Hours/Friends stats show hardcoded zeros.

**Files:**
- Modify: `kamee.app/kamee.app/Services/RoomService.cs`
- Modify: `kamee.app/kamee.app/ViewModels/ProfileViewModel.cs`
- Modify: `kamee.app/kamee.app/Views/Mobile/ProfilePage.xaml`

- [ ] **Step 1: Add `GetWatchHistoryAsync` to RoomService**

Add to `kamee.app/kamee.app/Services/RoomService.cs`:

```csharp
public async Task<List<WatchHistory>> GetWatchHistoryAsync(string userId)
{
    var response = await _supabase.Client
        .From<WatchHistory>()
        .Filter("user_id", Postgrest.Constants.Operator.Equals, userId)
        .Order("watched_at", Postgrest.Constants.Ordering.Descending)
        .Limit(20)
        .Get();
    return response.Models;
}

public async Task<int> GetRoomCountAsync(string userId)
{
    var response = await _supabase.Client
        .From<RoomMember>()
        .Filter("user_id", Postgrest.Constants.Operator.Equals, userId)
        .Get();
    return response.Models.Count;
}
```

- [ ] **Step 2: Update ProfileViewModel to load history and stats**

Replace `ProfileViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kamee.app.Models;
using kamee.app.Services;

namespace kamee.app.ViewModels
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly RoomService _roomService;

        [ObservableProperty]
        private User? _currentUser;

        [ObservableProperty]
        private ObservableCollection<WatchHistory> _watchHistory = new();

        [ObservableProperty]
        private int _roomCount;

        [ObservableProperty]
        private string? _errorMessage;

        public ProfileViewModel(AuthService authService, RoomService roomService)
        {
            _authService = authService;
            _roomService = roomService;
        }

        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                CurrentUser = await _authService.GetCurrentUserAsync();

                if (CurrentUser?.Id != null)
                {
                    var history = await _roomService.GetWatchHistoryAsync(CurrentUser.Id);
                    WatchHistory = new ObservableCollection<WatchHistory>(history);

                    RoomCount = await _roomService.GetRoomCountAsync(CurrentUser.Id);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SignOut()
        {
            await _authService.SignOutAsync();
            await Shell.Current.GoToAsync("//splash");
        }
    }
}
```

- [ ] **Step 3: Register RoomService in ProfileViewModel's DI — already done** (MauiProgram already registers `RoomService` and `ProfileViewModel` as Transient; `ProfileViewModel` constructor now takes `RoomService` so DI will inject it automatically.)

- [ ] **Step 4: Bind stats in ProfilePage.xaml**

In `kamee.app/kamee.app/Views/Mobile/ProfilePage.xaml`, replace the hardcoded stats section:

```xml
<!-- Stats row -->
<HorizontalStackLayout HorizontalOptions="Center" Spacing="28" Margin="0,8,0,0">
    <VerticalStackLayout HorizontalOptions="Center" Spacing="2">
        <Label Text="{Binding RoomCount}"
               FontSize="18" FontAttributes="Bold"
               TextColor="{StaticResource TextPrimary}"
               HorizontalOptions="Center" />
        <Label Text="Rooms" FontSize="10"
               TextColor="{StaticResource TextMuted}"
               HorizontalOptions="Center" />
    </VerticalStackLayout>
    <VerticalStackLayout HorizontalOptions="Center" Spacing="2">
        <Label Text="0" FontSize="18" FontAttributes="Bold"
               TextColor="{StaticResource TextPrimary}"
               HorizontalOptions="Center" />
        <Label Text="Hours" FontSize="10"
               TextColor="{StaticResource TextMuted}"
               HorizontalOptions="Center" />
    </VerticalStackLayout>
    <VerticalStackLayout HorizontalOptions="Center" Spacing="2">
        <Label Text="0" FontSize="18" FontAttributes="Bold"
               TextColor="{StaticResource TextPrimary}"
               HorizontalOptions="Center" />
        <Label Text="Friends" FontSize="10"
               TextColor="{StaticResource TextMuted}"
               HorizontalOptions="Center" />
    </VerticalStackLayout>
</HorizontalStackLayout>
```

- [ ] **Step 5: Call LoadAsync from ProfilePage.OnAppearing**

Check `kamee.app/kamee.app/Views/Mobile/ProfilePage.xaml.cs` — it must call `vm.LoadAsync()`. Replace the full file:

```csharp
using kamee.app.ViewModels;

namespace kamee.app.Views.Mobile
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage(ProfileViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetNavBarIsVisible(this, false);
            if (BindingContext is ProfileViewModel vm)
                await vm.LoadAsync();
        }
    }
}
```

- [ ] **Step 6: Build**

```
dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore
```

Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add kamee.app/kamee.app/Services/RoomService.cs
git add kamee.app/kamee.app/ViewModels/ProfileViewModel.cs
git add kamee.app/kamee.app/Views/Mobile/ProfilePage.xaml
git add kamee.app/kamee.app/Views/Mobile/ProfilePage.xaml.cs
git commit -m "feat: load watch history and room count on profile page"
```

---

### Task 7: Update viewer count on join/leave

`rooms.viewer_count` is never incremented or decremented. Supabase doesn't support atomic increments via the PostgREST client directly, so use a raw SQL RPC.

**Files:**
- Supabase Dashboard: create two SQL functions
- Modify: `kamee.app/kamee.app/Services/RoomService.cs`

- [ ] **Step 1: Create Supabase RPC functions**

Run in Supabase Dashboard → SQL Editor:

```sql
create or replace function increment_viewer_count(room_id uuid)
returns void language sql as $$
  update rooms set viewer_count = viewer_count + 1 where id = room_id;
$$;

create or replace function decrement_viewer_count(room_id uuid)
returns void language sql as $$
  update rooms set viewer_count = greatest(viewer_count - 1, 0) where id = room_id;
$$;
```

- [ ] **Step 2: Call RPCs from JoinRoomAsync and LeaveRoomAsync**

In `kamee.app/kamee.app/Services/RoomService.cs`, update `JoinRoomAsync`:

```csharp
public async Task JoinRoomAsync(string roomId, string userId)
{
    var member = new RoomMember
    {
        RoomId = roomId,
        UserId = userId,
        JoinedAt = DateTime.UtcNow
    };
    await _supabase.Client.From<RoomMember>().Insert(member);

    await _supabase.Client
        .From<User>()
        .Filter("id", Postgrest.Constants.Operator.Equals, userId)
        .Set(u => u.CurrentRoomId, roomId)
        .Update();

    await _supabase.Client.Rpc("increment_viewer_count",
        new Dictionary<string, object> { ["room_id"] = roomId });
}
```

Update `LeaveRoomAsync`:

```csharp
public async Task LeaveRoomAsync(string roomId, string userId)
{
    await _supabase.Client
        .From<RoomMember>()
        .Filter("room_id", Postgrest.Constants.Operator.Equals, roomId)
        .Filter("user_id", Postgrest.Constants.Operator.Equals, userId)
        .Delete();

    await _supabase.Client
        .From<User>()
        .Filter("id", Postgrest.Constants.Operator.Equals, userId)
        .Set(u => u.CurrentRoomId, (string?)null)
        .Update();

    await _supabase.Client.Rpc("decrement_viewer_count",
        new Dictionary<string, object> { ["room_id"] = roomId });
}
```

- [ ] **Step 3: Build**

```
dotnet build kamee.app/kamee.app.csproj -f net10.0-windows10.0.19041.0 --no-restore
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add kamee.app/kamee.app/Services/RoomService.cs
git commit -m "feat: increment/decrement viewer count via Supabase RPC on join/leave"
```

---

## Self-Review

**Spec coverage:**
- ✅ RoomId routing fixed (Task 1)
- ✅ Room name display (Task 2)
- ✅ Message sender names (Task 3)
- ✅ RoomCard tap (Task 4)
- ✅ Tab indicator (Task 5)
- ✅ Watch history + room count (Task 6)
- ✅ Viewer count (Task 7)
- ⏭ Hours watched — requires tracking watch session duration; deferred (no timer/session model)
- ⏭ Friends system — requires a separate `friendships` table and flow; deferred

**Placeholder scan:** No TBDs. All code blocks are complete. All file paths are exact.

**Type consistency:** `RoomService` methods added in Task 2, 6, 7 all use types already defined (`Room`, `WatchHistory`, `RoomMember`, `User`). `ProfileViewModel` uses `RoomService` which is already registered as Singleton in `MauiProgram.cs`. `ActiveTabColorConverter` registered in `AppStyles.xaml` and consumed in `HomePage.xaml`.
