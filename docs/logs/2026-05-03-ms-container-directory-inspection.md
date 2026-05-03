# 2026-05-03 MS Container Directory Inspection

## Summary

- Added a first `.ms` parser slice based on WC `Ms_File` / `Ms_FileV2` behavior.
- WzLib now reads `.ms` v2/Snow and v4/ChaCha20 container headers and entry
  tables.
- Core projects `.ms` entries into the shared inspection tree as image nodes
  with package identity, image selectors, and debug metadata for checksum,
  flags, relative block, offset, size, aligned size, and unknown fields.
- Later on 2026-05-03, `.ms` image payload extraction was added for the initial
  v2/Snow and v4/ChaCha20 slice; `wcx.package.ms.imageUnsupported` now means
  payload extraction or downstream IMG inspection failed for a specific entry.
- Unsupported `.ms` container shapes continue to report
  `wcx.package.ms.directoryUnsupported`.

## Validation

- Synthetic tests cover `.ms` v2/Snow and v4/ChaCha20 container directory
  inspection.
- Optional local GMS smoke with `WCX_GMS_DATA_DIR` now reads all 10 local
  `Data/Packs/*.ms` files without parser error diagnostics.
- Manual CLI smoke:

```bash
dotnet src/WzComparerX.Cli/bin/Debug/net10.0/WzComparerX.Cli.dll inspect --debug <local-gms-data>/Packs/Skill_00002.ms
```

Result summary: format `ms`, version `2`, entry count `57`, root `Skill`
directory, and image entries such as `15500.img` with offsets and sizes.

## Remaining Work

- Expand `.ms` entry payload coverage only when a new real sample or downstream
  feature exposes an unsupported payload/IMG shape.
- Define `List.wz` and `.mn` first-slice plans from WC reference behavior or an
  older-client sample.
- Keep PKG2 directory parsing blocked until a representative sample or
  synthetic shape is available.
