# 2026-06-01 - Canvas#Video MCV Header Inspection

## Context

The local GMS sample `Data/Packs/Mob_00002.ms` contains
`Mob/BossPattern/BossFirstAdversary.img` with a Canvas#Video value at
`1069/003/effect/0`. WCX already identified the value as `video`, but only
reported opaque payload offset and length metadata.

## Findings

- WC stores Canvas#Video payloads as `Wz_Video` blobs.
- `Wz_Video.ReadVideoFileHeader()` expects an `MCV0` signature, decodes the
  obfuscated fourCC, reads dimensions, frame count, data flags, timing defaults,
  and frame/alpha frame tables.
- WC plays frames with native VPX/YUV helpers. WCX does not add video playback
  or frame decode in this slice.

## Changes

- Added WzLib models for Canvas#Video `MCV0` header and frame table metadata.
- Parsed `MCV0` header metadata while keeping non-MCV or truncated payloads
  inspectable as opaque video blobs.
- Exposed video header fields through Core `inspect --debug` metadata:
  signature, fourCC, width, height, frame count, data flags, timing defaults,
  and first-frame offsets/counts.
- Updated the video diagnostic wording from generic payload decoding to frame
  decoding, since header parsing is now implemented.
- Added deterministic WzLib/Core/CLI coverage and an optional local GMS Packs
  smoke for `BossFirstAdversary.img`.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local GMS smoke:
  - `inspect --debug --json --key auto --depth 6 Data/Packs/Mob_00002.ms Mob/BossPattern/BossFirstAdversary.img`
    reports `1069/003/effect/0` as `MCV0`, `VP90`, `2656x1352`, `97` frames,
    alpha-map data, and first-frame table offsets.
