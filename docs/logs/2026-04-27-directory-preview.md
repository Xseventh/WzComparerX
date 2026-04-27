# 2026-04-27 Directory Preview

## Summary

Added the first real PKG1 directory pre-read path. This reads top-level entry
counts and raw entry metadata without decrypting names or resolving offsets.

## Completed

- Added `WzDirectoryPreviewReader` in `WzComparerX.WzLib`.
- Added preview models for directory entry kind, entry metadata, and document
  preview.
- Added Core service and text/JSON formatters.
- Added CLI command:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir path/to/file.wz
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --json path/to/file.wz
```

- Added deterministic byte-level WzLib tests.
- Added Core formatter test coverage.

## Local Smoke

Verified against:

```text
~/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Base/Base.wz
```

Summary:

```text
entries: 16
0..15 | directory | type=0x03
```

Names are intentionally not decoded yet. Real offset calculation is also not
implemented yet.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir "$HOME/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Base/Base.wz"
```

## Next Suggested Work

Migrate the minimal PKG1 string decryption needed to decode entry names for
`Base.wz`, then keep offset calculation as a separate follow-up.
