# 2026-05-03 MS/MN Reference And Plan Cleanup

## Summary

- Clarified the unsupported MS/MN diagnostic so it no longer says WCX only
  supports version 4 directory tables.
- Added synthetic `.mn` coverage through the same MS container reader path used
  for `.ms`, matching WC's `LoadMsFile` entry behavior.
- Recorded the WC reference files for MS/MN container directory inspection and
  the then-current directory-only boundary.
- Updated the M5 parser matrix with explicit next-slice decisions for MS/MN and
  `List.wz`.

## Notes

- `.ms` / `.mn` v2/Snow and v4/ChaCha20 container directory tables are the
  accepted M5 slice.
- Later on 2026-05-03, `.ms` / `.mn` image payload extraction was promoted out
  of this deferred state for the initial v2/v4 slice.
- `List.wz` should be treated as a string-list/key-profile helper, not as a
  resource package tree. Its first slice should decode WC-style list entries
  from deterministic synthetic streams before it affects automatic key
  detection.
- The current local GMS sample still has `.ms` files only; real `.mn` smoke
  remains blocked on an older-client sample.
