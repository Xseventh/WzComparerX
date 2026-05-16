# 2026-05-16 PKG1 Directory Edge Fixtures

- Added deterministic WzLib coverage for PKG1 directory tables whose entry
  count uses the expanded compressed-int32 encoding.
- Added deterministic WzLib coverage for PKG1 packages that carry an encrypted
  version field but whose hash offsets cannot be validated against the file
  bounds; WCX now locks the current behavior of leaving WZ/hash version and
  calculated offsets unset instead of inventing a candidate version.
- Updated the M5 parser coverage matrix so remaining PKG1 directory gaps are
  narrower: duplicate-name fixtures and any future real large-table smoke.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
