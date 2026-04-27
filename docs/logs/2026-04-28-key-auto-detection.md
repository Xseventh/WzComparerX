# 2026-04-28 - Preview String Key Auto-Detection

## Summary

Added `--key auto` for PKG1 preview workflows. The CLI can now try no-op, KMS,
and GMS string decoding for directory previews and select the most plausible
result with deterministic name scoring.

## Changes

- Added Core string-key auto-detection for `preview-dir`.
- Reused detected directory key selection when `preview-img --key auto` reads an
  IMG payload.
- Recorded the selected `stringKey` in directory previews and text output.
- Added a deterministic Core test for no-op key auto-detection.
- Documented the command usage and local GMS smoke behavior.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local smoke:
  - `preview-dir --key auto` on local `Data/Base/Base.wz`
  - `preview-img --key auto --depth 2` on local `Data/Base/Base_000.wz`
