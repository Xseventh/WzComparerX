# 2026-05-17 Core Value Projection Helper

## Context

M5 keeps adding IMG value families, payload metadata, and stable diagnostics.
`ResourceInspectionService` had become the place where image value metadata and
payload diagnostics were projected, which made the service harder to keep as a
workflow coordinator.

## Changes

- Extracted IMG value metadata projection into `ResourceInspectionValueProjection`.
- Moved Canvas / RawData / Video / Sound diagnostics selection into the same
  helper.
- Kept `ResourceInspectionService` responsible for composing inspections,
  identities, and links while delegating value-family projection.

## Validation

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless
```

All commands passed.
