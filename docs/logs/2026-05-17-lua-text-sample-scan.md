# 2026-05-17 Lua Text Sample Scan

## Context

M5 still tracks Lua IMG and WC text-format IMG as fixture-backed parser slices
without direct real-client smoke.

## Findings

- A parser-based directory scan across the local GMS common package families
  found no `.lua` image entries:
  - `UI`
  - `Skill`
  - `Effect`
  - `Item`
  - `Character`
  - `String`
  - `Map`
  - `Mob`
  - `Npc`
  - `Packs`
- This confirms the current local GMS sample cannot provide a direct Lua IMG
  smoke through the supported inspection path.
- Text-format IMG discovery cannot rely on directory names because the
  supported signatures are payload-level (`#Property` and `Root`). A small
  bounded probe did not find a sample before exceeding the useful scan budget,
  so broad text-format discovery remains deferred until a likely package or
  older-client sample is available.

## Notes

- Lua/text parser behavior remains covered by committed synthetic fixtures and
  CLI golden outputs.
- No client files were added to the repository.
