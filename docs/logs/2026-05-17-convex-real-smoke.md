# 2026-05-17 Convex Real Smoke

## Context

M5 still had `Shape2D#Convex2D` covered only by synthetic fixtures. A parser-based
local-client scan is needed because WZ/IMG type names are encoded or referenced,
so plain binary text search is not reliable.

## Changes

- Added optional external-client Core smoke for `Data/UI/UI_000.wz`, selector
  `RunnerGame.img`.
- The smoke validates `RunnerGameUI/Object/0/Tile/0/foothold` as a real
  `Shape2D#Convex2D` value with four points:
  `(-39, 0)`, `(-39, -25)`, `(39, -25)`, and `(39, 0)`.
- Updated parser coverage and format notes to move Convex2D from
  synthetic-only to local GMS real-smoke covered.

## Validation

Run the targeted smoke with a local client `Data` directory:

```bash
WCX_CLIENT_DATA_DIR=/path/to/Data dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~InspectOptionalExternalClientUiImage_ReadsConvex2DMetadata
```

The targeted smoke passed against the local GMS client. Full solution build/test
and the standard App Headless subset also passed before commit.
