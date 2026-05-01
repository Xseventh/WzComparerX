# 2026-05-02 M5 Media Payload Diagnostics

## Summary

- Locked the current RawData, Video, and Sound_DX8 payload behavior through
  Core and CLI tests.
- Confirmed the M5 contract: WCX inspects media payload metadata now, while
  decode/export/playback remain later work with stable unsupported diagnostics.
- Tightened the CLI test fixture helper so larger synthetic IMG entries use the
  same compressed-int size encoding as the parser expects.

## Behavior

- `inspect --debug` reports RawData, Video, and Sound_DX8 payload metadata such
  as data offset, data length, sound duration, and sound declaration.
- The same nodes carry parser `info` diagnostics:
  - `wcx.payload.rawData.unsupported`
  - `wcx.payload.video.unsupported`
  - `wcx.payload.audio.unsupported`
- These diagnostics remain non-fatal for `inspect` because metadata inspection
  succeeds; future media export/playback should reuse the same diagnostic
  families when requested content cannot be produced.

## Verification

- Passed: `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
  - 0 warnings, 0 errors.
- Passed: `dotnet test WzComparerX.slnx --no-build -m:1`
  - 165 total, 0 failed.
