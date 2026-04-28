# 2026-04-28 - Lua Image Inspection

## Summary

Added a WC-compatible inspection path for IMG entries whose names end in `.lua`.
Lua images are not normal IMG object-tag streams, so `inspect` now handles
their block format separately.

## Changes

- Added key-stream-only payload decryption support to `WzStringDecryptor`.
- Added `WzImageLuaInspection` for Lua payload length and short text snippets.
- Added Lua block parsing for `.lua` image entries.
- Preserved `--depth 0` behavior so Lua payloads are not read when only the
  object type is requested.
- Added deterministic WzLib tests for Lua inspection and depth-zero behavior.
- Documented the WC Lua stream shape and current export limitation.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local exploration:
  - `inspect --key auto` on local `Data/UI/WZ2Lua/WZ2Lua.wz`
  - `inspect --key auto` on local `_Canvas` packages containing `.lua`
    strings

No direct local `.lua` image entry was exposed by the currently supported
directory inspection paths; the parser behavior is locked with synthetic fixtures.
