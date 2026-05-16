# 2026-05-16 Unified External Client Smoke

- Added `ExternalClientSmokeData` as the shared test harness entry for optional
  local client smoke tests.
- Added `WCX_CLIENT_DATA_DIRS` for one or more client `Data` directories.
- Removed region-specific external smoke variables; use labels such as
  `gms=<path>` and `kms=<path>` inside `WCX_CLIENT_DATA_DIRS` when multiple
  clients need to be identified in one run.
- Moved Core smoke tests from `ResourceInspectionGmsSmokeTests` to
  `ResourceInspectionExternalClientSmokeTests`, with capability-style file
  discovery instead of assuming every configured client is GMS.
- Moved the optional Avalonia UI smoke to the same external-client lookup path.
- Kept KMS full-client smoke as a TODO until a complete KMS client is available;
  future KMS cases should extend the same external-client harness instead of
  adding a separate region-specific test runner.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- `WCX_CLIENT_DATA_DIRS=<local GMS Data directory> dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~ResourceInspectionExternalClientSmokeTests`
- `WCX_CLIENT_DATA_DIRS=<local GMS Data directory> dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~MainWindow_OptionalExternalClientMapPackageGroupSmoke`
