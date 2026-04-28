# 2026-04-28 - PKG1 Key Name Terminology

## Summary

- Renamed WCX's no-op PKG1 string key mode from `Bms` to `None`.
- Updated `inspect` usage and docs to describe the default as `--key none`.
- Kept `--key bms` as a compatibility alias for WC's historical no-op key name.

## Notes

`BMS` in WC's crypto key enum maps to `Wz_NonOpCryptoKey`, so WCX should not
present it as a MapleStory service-region name. User-facing commands now use
`none|kms|gms` terminology while preserving the older alias for migration work.
