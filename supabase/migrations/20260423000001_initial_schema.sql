-- Kamee: initial schema
-- Run this first in Supabase SQL Editor (or via supabase db push)

-- ─── profiles ───────────────────────────────────────────────────────────────
-- Mirrors auth.users; created automatically on sign-up via trigger below.
create table if not exists public.profiles (
  id              uuid        primary key references auth.users on delete cascade,
  username        text        not null unique,
  avatar_url      text,
  is_online       boolean     not null default false,
  current_room_id uuid,       -- FK added after rooms table is created
  created_at      timestamptz not null default now()
);

-- ─── rooms ──────────────────────────────────────────────────────────────────
create table if not exists public.rooms (
  id                  uuid        primary key default gen_random_uuid(),
  name                text        not null,
  host_id             uuid        not null references public.profiles on delete cascade,
  streaming_platform  text        not null default '',
  video_url           text,
  is_private          boolean     not null default false,
  is_live             boolean     not null default true,
  viewer_count        integer     not null default 0,
  created_at          timestamptz not null default now()
);

-- Add FK from profiles.current_room_id → rooms now that rooms exists
alter table public.profiles
  add constraint profiles_current_room_id_fkey
  foreign key (current_room_id) references public.rooms on delete set null
  not valid;  -- skip validation on existing rows

-- ─── room_members ────────────────────────────────────────────────────────────
create table if not exists public.room_members (
  room_id    uuid        not null references public.rooms on delete cascade,
  user_id    uuid        not null references public.profiles on delete cascade,
  joined_at  timestamptz not null default now(),
  primary key (room_id, user_id)
);

-- ─── messages ────────────────────────────────────────────────────────────────
create table if not exists public.messages (
  id       uuid        primary key default gen_random_uuid(),
  room_id  uuid        not null references public.rooms on delete cascade,
  user_id  uuid        not null references public.profiles on delete cascade,
  content  text        not null,
  sent_at  timestamptz not null default now()
);

-- ─── watch_history ───────────────────────────────────────────────────────────
create table if not exists public.watch_history (
  id             uuid        primary key default gen_random_uuid(),
  user_id        uuid        not null references public.profiles on delete cascade,
  room_id        uuid        references public.rooms on delete set null,
  title          text,
  thumbnail_url  text,
  watched_at     timestamptz not null default now()
);

-- ─── Auto-create profile on sign-up ─────────────────────────────────────────
create or replace function public.handle_new_user()
returns trigger language plpgsql security definer as $$
begin
  insert into public.profiles (id, username, created_at)
  values (
    new.id,
    coalesce(new.raw_user_meta_data->>'username', split_part(new.email, '@', 1)),
    now()
  );
  return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
  after insert on auth.users
  for each row execute procedure public.handle_new_user();

-- ─── Row Level Security ───────────────────────────────────────────────────────
alter table public.profiles     enable row level security;
alter table public.rooms        enable row level security;
alter table public.room_members enable row level security;
alter table public.messages     enable row level security;
alter table public.watch_history enable row level security;

-- profiles: anyone can read; only owner can insert/update their own row
create policy "profiles_select" on public.profiles for select using (true);
create policy "profiles_insert" on public.profiles for insert with check (auth.uid() = id);
create policy "profiles_update" on public.profiles for update using (auth.uid() = id);

-- rooms: anyone can read; only host can update/delete
create policy "rooms_select" on public.rooms for select using (true);
create policy "rooms_insert" on public.rooms for insert with check (auth.uid() = host_id);
create policy "rooms_update" on public.rooms for update using (auth.uid() = host_id);
create policy "rooms_delete" on public.rooms for delete using (auth.uid() = host_id);

-- room_members: members can read; users can insert/delete their own rows
create policy "room_members_select" on public.room_members for select using (true);
create policy "room_members_insert" on public.room_members for insert with check (auth.uid() = user_id);
create policy "room_members_delete" on public.room_members for delete using (auth.uid() = user_id);

-- messages: anyone in the room can read; only sender can insert
create policy "messages_select" on public.messages for select using (true);
create policy "messages_insert" on public.messages for insert with check (auth.uid() = user_id);

-- watch_history: only owner can read/write
create policy "watch_history_select" on public.watch_history for select using (auth.uid() = user_id);
create policy "watch_history_insert" on public.watch_history for insert with check (auth.uid() = user_id);

-- ─── Realtime ────────────────────────────────────────────────────────────────
-- Enable realtime for messages (needed for ChatService live updates)
alter publication supabase_realtime add table public.messages;
