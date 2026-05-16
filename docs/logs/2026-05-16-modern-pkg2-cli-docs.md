# 2026-05-16 Modern PKG2 CLI And Docs Follow-Up

- Added CLI-level coverage for modern KMS PKG2 header and `inspect --debug`
  output using the synthetic modern PKG2 fixture.
- Locked the current debug contract for modern KMS PKG2:
  - `formatProfile: pkg2_modern_kms`
  - `pkg2HeaderVariant: modern`
  - `hashVersion: 2967082738`
  - no synthetic `wzVersion`
- Updated M5 parser documentation to include the modern KMS 0x44-byte PKG2
  envelope alongside the earlier KMST1199/1200 PKG2 slice.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter "FullyQualifiedName~ModernKmsPkg2|FullyQualifiedName~HeaderModernKmsPkg2|FullyQualifiedName~InspectDebugModernKmsPkg2"`
- `dotnet test WzComparerX.slnx --no-build -m:1`
