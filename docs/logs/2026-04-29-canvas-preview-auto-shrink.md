# 2026-04-29 - Canvas Preview Auto Shrink

## Summary

Adjusted Avalonia Canvas preview `Auto` scale so very large Canvas bitmaps no
longer open at full size by default.

## Changes

- Kept manual preview scales as exact integer buttons: `1x`, `2x`, `4x`, `8x`,
  and `16x`.
- Changed `Auto` scale from an integer-only value to a display ratio so it can
  still enlarge small sprites while proportionally shrinking large Canvas
  bitmaps.
- Large Canvas previews now target roughly a 960-pixel longest side once the
  source bitmap is above the large-image threshold.
- Added ViewModel coverage for the large-image shrink path and updated existing
  scale assertions for the ratio-based model.
- Updated M4 docs to describe both small-image enlargement and large-image
  Auto shrink.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
