create policy "room_members_update" on public.room_members
  for update using (auth.uid() = user_id);
