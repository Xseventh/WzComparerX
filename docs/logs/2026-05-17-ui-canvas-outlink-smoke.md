# 2026-05-17 UI Canvas Outlink Smoke

## Context

M5 tracks real-client evidence for link-heavy package families. Map, Character,
Effect, and MS pack `_outlink` paths already had optional local GMS smoke, while
the UI row in `docs/parser-coverage-matrix.md` still only claimed directory and
Canvas metadata coverage.

## Changes

- Added optional external-client Core smoke for `Data/UI/UI_000.wz`, selector
  `Basic.img`.
- The inspection smoke verifies `Cursor/0/0/_outlink` keeps the logical target
  `UI/_Canvas/Basic.img/Cursor/0/0` and resolves it to
  `Data/UI/_Canvas/_Canvas_000.wz`, image selector `Basic.img`, value path
  `Cursor/0/0`.
- The Canvas preview smoke loads the same link through
  `ResourceCanvasImageService` and verifies the linked 24x28 format-1 direct-zlib
  pixels.
- Updated parser coverage notes and handoff text so UI `_Canvas` link coverage
  no longer appears as an open M5 gap.

## Validation

Targeted optional GMS smoke was run with `WCX_CLIENT_DATA_DIR` pointing at the
local GMS `Data` directory:

```bash
dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj -m:1 --filter "FullyQualifiedName~InspectOptionalExternalClientUiImage_ResolvesCanvasOutlinkIdentity|FullyQualifiedName~CanvasOptionalExternalClientUiImage_ResolvesOutlinkPreview"
```

Both targeted tests passed. Full solution build/test and the App Headless subset
also passed before commit.
