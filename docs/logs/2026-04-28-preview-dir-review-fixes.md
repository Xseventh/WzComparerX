# 2026-04-28 - Preview Dir Review Fixes

## Summary

- Changed `preview-dir` to return exit code `1` when the input header is
  invalid, matching the `header` command's failure behavior.
- Implemented PKG1 `0x02` string-reference name decoding using WC's offset
  adjustment rules.
- Added deterministic WzLib coverage for referenced string names.

## Notes

For PKG1 `0x02` entries, WC reads the following 32-bit value as a string
reference and calls `ReadStringAt(value + stringOffAdd)`. `stringOffAdd` is `-1`
for normal PKG1 files and `+2` for missing-encrypted-version files.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir /tmp/wcx-invalid-preview.wz
/usr/local/share/dotnet/dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir path/to/Data/Base/Base.wz
```
