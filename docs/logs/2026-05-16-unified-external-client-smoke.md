# 2026-05-16 Unified External Client Smoke

- Added `ExternalClientSmokeData` as the shared test harness entry for optional
  local client smoke tests.
- Added `WCX_CLIENT_DATA_DIR` for one client `Data` directory.
- Removed region-specific external smoke variables. Region/client family is not
  part of the smoke-test input; parser behavior should be selected from the
  file header/container shape.
- Moved Core smoke tests from `ResourceInspectionGmsSmokeTests` to
  `ResourceInspectionExternalClientSmokeTests`, with capability-style file
  discovery instead of assuming every configured client is GMS.
- Moved the optional Avalonia UI smoke to the same external-client lookup path.
- Kept KMS full-client smoke as a TODO until a complete KMS client is available;
  future KMS cases should extend the same external-client harness instead of
  adding a separate region-specific test runner.
- Added deterministic tests for external-client path parsing so valid,
  missing, and empty paths stay stable without mutating process environment
  variables during xUnit runs.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- `WCX_CLIENT_DATA_DIR=<local GMS Data directory> dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~ResourceInspectionExternalClientSmokeTests`
- `WCX_CLIENT_DATA_DIR=<local GMS Data directory> dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~MainWindow_OptionalExternalClientMapPackageGroupSmoke`
