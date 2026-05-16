# 2026-05-16 Optional KMS PKG2 Smoke

- Added optional Core smoke tests gated by `WCX_KMS_DATA_DIR`.
- The tests do not read external files when the environment variable is unset.
- When pointed at a modern KMS PKG2 sample directory, the smoke covers:
  - `Item_000.wz` modern PKG2 header variant and root image directory table;
  - `ItemSellPriceStandard.img` image payload extraction through the shared IMG
    inspection path;
  - `String_000.wz` root image directory table.
- Documented the optional KMS smoke entry in README, handoff, and format notes.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter "FullyQualifiedName~OptionalKms"`
- `WCX_KMS_DATA_DIR=/Users/seventh/Downloads/new_kms dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter "FullyQualifiedName~OptionalKms"`
- `dotnet test WzComparerX.slnx --no-build -m:1`
