# 2026-04-28 - Top-Level IMG Object Inspection

## Summary

Extended `inspect` so supported non-`Property` IMG root objects now expose
their inspection value or metadata directly.

## Changes

- Added `ObjectValue` to `WzImageInspection`.
- Reused existing IMG object readers for top-level `Canvas`, `Shape2D#Vector2D`,
  `Shape2D#Convex2D`, `UOL`, `RawData`, `Canvas#Video`, and `Sound_DX8`
  objects.
- Updated text output to print top-level `objectValue`.
- Added deterministic WzLib tests for top-level Canvas metadata and Vector
  values.
- Documented the behavior in command and migration notes.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local smoke:
  - `inspect --key auto` on local `Data/UI/UI_000.wz`
  - `inspect --key auto --depth 2` on local `Data/UI/UI_000.wz` `Basic.img`
