# 2026-04-27 Header Scan Command

## Summary

Added a recursive WZ header scan command and verified it against the local
MapleStoryNA client.

## Completed

- Added `WzPackageHeaderScanService`.
- Added text and JSON scan formatters.
- Added CLI command:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- headers path/to/file-or-directory
dotnet run --project src/WzComparerX.Cli --no-build -- headers --json path/to/file-or-directory
```

- Added tests for deterministic sorted directory scans and JSON scan output.
- Documented the local client path as a manual smoke source only.

## Local Smoke

The local MapleStoryNA client data path:

```text
~/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data
```

Verified:

```bash
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- headers "$HOME/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Base"
```

Output summary:

```text
files: 2
ok | pkg1 | 316 | .../Data/Base/Base.wz
ok | pkg1 | 6469 | .../Data/Base/Base_000.wz
```

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
```

## Next Suggested Work

Use the small local `Data/Base` files to guide the first directory-entry
pre-read behavior, then add synthetic byte-level tests before attempting full
directory tree parsing.
