# 2026-05-02 GMS Core Smoke Tests

## Summary

- Added optional Core smoke tests for the local GMS client data directory.
- The tests are gated by `WCX_GMS_DATA_DIR`; without that environment variable
  they return without touching external files, so normal CI remains
  deterministic.
- The tests keep client files outside git and only perform read-only
  inspection.

## Coverage

- Representative package roots:
  - `Character/Character.wz`
  - `Effect/Effect.wz`
  - `Item/Item.wz`
  - `Mob/Mob.wz`
  - `Npc/Npc.wz`
  - `Skill/Skill.wz`
  - `Sound/Sound.wz`
  - `String/String.wz`
  - `UI/UI.wz`
- Map package group:
  - `Map/Map/Map1/Map1.wz` exposes merged `100000000.img`.
  - The merged image identity points back to `Map1_000.wz`.
- Map link identity:
  - `Map/Map/Map1/Map1_000.wz` `100000000.img` exposes a `miniMap/_outlink`.
  - `inspect --debug` resolves that link to image selector `100000000.img` and
    value path `miniMap/canvas`.

## Verification

- Passed: `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj -m:1`
  - 74 total, 0 failed.
- Passed with local GMS data:
  `WCX_GMS_DATA_DIR="<local GMS Data directory>" dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~ResourceInspectionGmsSmokeTests`
  - 3 total, 0 failed.
- Passed: `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
  - 0 warnings, 0 errors.
- Passed: `dotnet test WzComparerX.slnx --no-build -m:1`
  - 175 total, 0 failed.
- Passed: `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless`
  - 13 total, 0 failed.
