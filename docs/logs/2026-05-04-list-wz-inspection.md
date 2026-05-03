# 2026-05-04 List.wz Inspection

## Summary

- Added a WzLib `List.wz` reader/model based on WC's
  `Wz_Crypto.LoadListWz` behavior.
- The reader decodes WC-style records for no-op, KMS, and GMS key profiles,
  excludes the `dummy` sentinel from effective entries, and auto-detects the
  key for synthetic coverage.
- Projected `List.wz` through Core `inspect` as `format: listwz` with compact
  normal output and debug metadata for entry count, selected string key, record
  offsets, and character counts.

## Scope Boundary

`List.wz` is a string-list/key-profile helper, not a WZ package tree. This
iteration makes it observable through the same `inspect` surface as other
parser slices, but it does not yet feed the decoded list into PKG1 string
key/profile selection. The current local GMS client has no `List.wz`, so real
smoke remains blocked on an older-client sample.

## Verification

- `dotnet test tests/WzComparerX.WzLib.Tests/WzComparerX.WzLib.Tests.csproj -m:1 --filter FullyQualifiedName~WzListFileReaderTests`
- `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj -m:1 --filter FullyQualifiedName~InspectDebugListWz`
