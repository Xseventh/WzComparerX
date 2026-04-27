# 2026-04-28 PKG1 String Names

## Summary

Added PKG1 directory string decoding for previewed directory entries.

## Completed

- Added `WzStringDecryptor` and `WzStringEncryptionKind`.
- Implemented BMS/no-op, KMS, and GMS PKG1 string key modes.
- Updated directory preview entries to include decoded names.
- Added CLI key selection:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --key bms path/to/file.wz
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --key kms path/to/file.wz
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --key gms path/to/file.wz
```

- Defaulted `preview-dir` to BMS/no-op because the local MapleStoryNA client
  `Data/Base/Base.wz` decodes correctly with that key.
- Updated synthetic byte tests to use encoded string bytes instead of plaintext.

## Local Smoke

Verified:

```bash
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir "$HOME/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data/Base/Base.wz"
```

Output now includes names:

```text
entries: 16
1 | directory | name=Character
2 | directory | name=Effect
3 | directory | name=Etc
...
15 | directory | name=UI
```

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
```

## Next Suggested Work

Migrate PKG1 version hash and offset calculation so decoded entries can point
to real child directory/image payload locations.
