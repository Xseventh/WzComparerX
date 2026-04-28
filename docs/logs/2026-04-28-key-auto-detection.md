# 2026-04-28 - Inspection String Key Auto-Detection

## Summary

Added `--key auto` for PKG1 inspection workflows. The CLI can now try no-op, KMS,
and GMS string decoding for directory inspections and select the most plausible
result with deterministic name scoring.

## Changes

- Added Core string-key auto-detection for `inspect`.
- Reused detected directory key selection when `inspect --key auto` reads an
  IMG payload.
- Recorded the selected `stringKey` in directory inspections and text output.
- Added a deterministic Core test for no-op key auto-detection.
- Documented the command usage and local GMS smoke behavior.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local smoke:
  - `inspect --key auto` on local `Data/Base/Base.wz`
  - `inspect --key auto --depth 2` on local `Data/Base/Base_000.wz`
