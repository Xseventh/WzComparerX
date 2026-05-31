# 2026-06-01 - Debug Link Resolution Cache

## Context

`inspect --debug --json --depth 3` on the updated local GMS
`Data/Packs/Mob_00000.ms` selector `Mob/8881671.img` appeared to hang. The
command completed, but took about 38 seconds.

## Findings

- `--depth 1` and `--depth 2` completed in under a second.
- `--depth 3` without `--debug` also completed in under a second.
- The slow path was Core debug projection, not MS binary decoding or JSON
  serialization.
- The image expands to hundreds of link-like values at depth 3. Debug projection
  resolved each `_outlink` independently, repeatedly re-reading the same MS
  containers and WZ package groups to verify target paths.

## Changes

- Added a per-inspection link resolution cache in Core.
- Cached MS/MN container inspections and WZ package group inspections while
  projecting one IMG inspection document.
- Kept resolver behavior unchanged: cached results are only reused within the
  current inspection call.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- Local GMS smoke:
  - Before cache: `inspect --debug --json --key auto --depth 3 ... Mob/8881671.img`
    took about 38 seconds and emitted about 10 MB JSON.
  - After cache: the same command took about 1.2 seconds with the same output
    size.
