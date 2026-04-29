# 2026-04-29 - Linked Image Selection

## Summary

Fixed Avalonia IMG inspection for image nodes that come from linked
split-package trees.

## Changes

- Added package/image target resolution for selectors shaped like
  `<package>.wz/<image>.img`.
- Selecting an image node under a linked package now switches `PathText` to the
  actual linked package and keeps only the IMG path in `SelectorText`.
- Manually pasting an absolute `<package>.wz/<image>.img` path into the IMG
  field now follows the same path.
- Kept the old package-root prefix behavior for selectors such as
  `Base_000.wz/StandardPDD.img`.
- Added ViewModel tests for linked package image inspection and manual embedded
  package selectors.
- Let pure ViewModel tests inject a Canvas preview factory so they do not need
  an Avalonia render backend just to verify selection state.

## Notes

Single-selecting a directory image node still does not decode IMG payloads by
itself. This keeps IMG loading lazy: use `Inspect Image` or double-click the
image node, then select a Canvas value or a root Canvas IMG to preview pixels.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build --filter "FullyQualifiedName~MainWindowViewModelTests"
```

Results:

- Build passed with 0 warnings and 0 errors.
- ViewModel tests passed: 27 total.
- Full test suite passed after this fix: App 40, Core 53, WzLib 44; total 137.
- Local read-only GMS smoke: `Npc/_Canvas/_Canvas_000.wz` image
  `0002000.img` inspects successfully and exposes nested format `1` Canvas
  values such as `stand/0`, `finger/0`, and `wink/0`.
