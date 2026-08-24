# 2026-08-24 CMS v227 Client Smoke

## Summary

- Ran read-only parser and UI smoke validation against a local CMS v227.7 client
  `Data` directory through the unified `WCX_CLIENT_DATA_DIR` entrypoint.
- Adjusted optional external-client smoke assertions so they validate portable
  parser behavior instead of GMS-specific shard numbers, MS container version,
  or payload offsets.

## Coverage

- CMS layout contains hundreds of WZ packages, many `.ini` package groups, and
  two MS packs under `Data/Packs`.
- `Base/Base.wz` inspects as PKG1 with `wzVersion` 227 and links into sibling
  package folders.
- `Map/Map/Map1/Map1.wz` merges numbered shard entries from `Map1_000.wz`;
  image identities preserve the true source package.
- `Map1_000.wz` `100000000.img` resolves `miniMap/canvas/_outlink` into the
  local `_Canvas_000.wz` package group.
- `Packs/Mob_00000.ms` and `Packs/Skill_00000.ms` inspect as MS v4 containers,
  and representative image payloads extract through the shared IMG inspection
  path.
- `UI/UI_000.wz` `Login.img` exposes RawData metadata and stable unsupported
  payload diagnostics.
- `Effect/Effect_000.wz` `BasicEff.img` resolves Canvas `_outlink` values into
  the local Effect `_Canvas` package group.

## Validation

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~ResourceInspectionExternalClientSmokeTests -e WCX_CLIENT_DATA_DIR=<local CMS Data directory>`
- `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~MainWindowExternalClientSmokeTests -e WCX_CLIENT_DATA_DIR=<local CMS Data directory>`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- CLI smoke with `inspect --debug --json --key auto` against representative
  CMS PKG1, split-package, MS v4, RawData, and Canvas link samples.

## Result

- Core external-client smoke passed: 22 total, 0 failed.
- App Headless external-client smoke passed: 3 total, 0 failed.
- Full solution tests passed:
  - App: 67 total, 0 failed.
  - Core: 143 total, 0 failed.
  - Rendering: 21 total, 0 failed.
  - WzLib: 78 total, 0 failed.

## Notes

- The client files and local smoke outputs remain outside the repository.
- `header` remains a WZ package header command; `.ms` containers are validated
  through `inspect`.
