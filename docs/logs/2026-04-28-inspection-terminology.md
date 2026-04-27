# 2026-04-28 - Inspection Terminology

## Summary

Removed the old migration-era preview terminology from active parser and
inspection boundaries.

## Changes

- Renamed WzLib directory and IMG parser DTOs/readers from `Preview` names to
  `Inspection` names.
- Updated Core inspection projection and string-key detection to use the neutral
  WzLib inspection types directly.
- Renamed WzLib tests to match the inspection terminology.
- Cleaned active docs so `inspect` and `inspect --debug` remain the supported
  concepts.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`

## Notes

Historical iteration logs may still mention previous command names because they
record past work. Current code, tests, command docs, roadmap, format notes, and
handoff use inspection terminology.
