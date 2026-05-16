# 2026-05-16 Defer KMS PKG2 Smoke

- Removed the premature `WCX_KMS_DATA_DIR` optional Core smoke tests.
- Kept modern KMS PKG2 coverage in deterministic synthetic fixtures and CLI
  tests.
- Documented the future `WCX_KMS_DATA_DIR` smoke as a TODO for when a complete
  KMS client is available.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
