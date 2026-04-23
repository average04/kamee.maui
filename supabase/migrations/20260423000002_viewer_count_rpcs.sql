-- Viewer count RPCs
-- Called by RoomService.JoinRoomAsync and LeaveRoomAsync via Supabase client .Rpc()

create or replace function public.increment_viewer_count(room_id uuid)
returns void language sql security definer as $$
  update public.rooms
  set viewer_count = viewer_count + 1
  where id = room_id;
$$;

create or replace function public.decrement_viewer_count(room_id uuid)
returns void language sql security definer as $$
  update public.rooms
  set viewer_count = greatest(viewer_count - 1, 0)
  where id = room_id;
$$;
