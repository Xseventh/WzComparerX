# 2026-05-15 Modern PKG2 Header

- Added WCX recognition for the current KMS 0x44-byte PKG2 envelope. The reader gathers `hash1`, the V4-style check value, and `dataSize` from the scattered A/B/C byte positions and validates the check with fixed hashVersion `0xB0DA16F2`.
- Kept the container modeled as `WzPackageFormat.Pkg2`; only the header envelope differs. Directory parsing continues through the existing PKG2 directory reader with `HeaderSize = DirectoryStartPosition = 0x44`.
- Added a `pkg2_modern_kms` directory profile. It derives the PKG2 string key from `hash1` plus the fixed hashVersion instead of treating the modern header as KMST1199.
- Added synthetic modern-PKG2 fixtures and WzLib/Core coverage for header reading and directory inspection. Real client WZ files remain local-only and are not added to the repository.

Verification:

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local smoke only: `inspect --debug --key none` successfully lists `/Users/seventh/Downloads/new_kms/Item_000.wz` (4 entries) and `/Users/seventh/Downloads/new_kms/String_000.wz` (26 entries); `ItemSellPriceStandard.img` also reads as an image payload.
