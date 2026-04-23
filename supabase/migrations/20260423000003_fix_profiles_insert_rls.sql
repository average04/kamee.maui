-- Fix: profiles table was missing an INSERT policy.
-- Upsert from AuthService.SignUpAsync requires INSERT + UPDATE permissions.
create policy "profiles_insert" on public.profiles for insert with check (auth.uid() = id);
