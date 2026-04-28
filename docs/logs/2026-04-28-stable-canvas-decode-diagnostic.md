# 2026-04-28 - Stable Canvas Decode Diagnostic

## Summary

- Stabilized the Canvas export decode-failure diagnostic message.
- Kept raw zlib/stream exception text out of user-visible diagnostics for now.
- Added an exact stderr golden fixture for `wcx.export.canvas.decodeFailed`.
- Documented that diagnostics should avoid volatile exception text unless a
  structured field or debug surface is added first.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
