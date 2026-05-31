# 2026-06-01 - MS ChaCha20 Entry Table Alignment

## Context

After a local GMS client update, `Data/Packs/Mob_00000.ms` stopped inspecting:
WCX reported `wcx.package.ms.directoryUnsupported` before any IMG payload was
read.

## Findings

- The file still matched the existing v4/ChaCha20 header shape.
- `Mob_00000.ms` decoded `version = 4`, `saltLength = 22`, and
  `entryCount = 7025`.
- The failure happened while reading the encrypted entry table, not while
  reading an IMG payload or Canvas value.
- WC's `Ms_FileV2.ChaCha20Reader` keeps ChaCha20 state across 64-byte blocks and
  resets the counter only when a logical read call ends exactly on a block
  boundary. WCX was resetting to counter zero for every block, which decoded the
  first entry but corrupted later entries once the table crossed a block.

## Changes

- Added a counter-aware `WzMsChaCha20.XorBlock` overload.
- Updated the v4 MS entry table reader to match WC's chunk/reset behavior.
- Updated synthetic v4 MS fixture generation to use the same reader boundary
  semantics.
- Added a deterministic WzLib regression test for v4 MS entry tables spanning
  ChaCha20 blocks.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local GMS smoke:
  - `inspect --debug --key none --depth 1 Data/Packs/Mob_00000.ms` now reports
    `format: ms`, `version: 4`, and `entryCount: 7025`.
  - `inspect --key none --depth 2 Data/Packs/Mob_00000.ms Mob/1150000.img`
    reaches the shared IMG inspection path and lists `info`, `move`, `stand`,
    `hit1`, and `die1` nodes.
