alter table public.rooms
  add column if not exists is_live boolean not null default true;
