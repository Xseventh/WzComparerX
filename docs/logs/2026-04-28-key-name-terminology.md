# 2026-04-28 - PKG1 Key Name Terminology

## Summary

- Renamed WCX's no-op PKG1 string key mode from `Bms` to `None`.
- Updated `inspect` usage and docs to describe the default as `--key none`.
- Removed the ambiguous legacy alias from the current CLI surface.

## Notes

The no-op crypto key should not be presented as a MapleStory service-region
name. User-facing commands now use `none|noop|kms|gms` terminology.
