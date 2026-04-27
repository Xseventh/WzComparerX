# 2026-04-28 - Text IMG Preview

## Summary

Added preview support for WC's text-format IMG streams. These streams are not
binary IMG object-tag data; they are textual property trees.

## Changes

- Recognize text IMG v1 payloads that start with `#Property`.
- Recognize text IMG v2 payloads that start with `Root <Property>`.
- Parse scalar text values into the existing property preview model.
- Parse nested property blocks with the same depth bound used by binary IMG
  previews.
- Added deterministic WzLib tests for v1 and v2 text previews.
- Documented the behavior and current limitations.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local exploration:
  - searched local GMS `Data` for `#Property` and `Root <Property>`
- Local smoke:
  - `preview-img --key auto --depth 2` on local `Data/UI/UI_000.wz` `Basic.img`

No direct local text IMG smoke sample was found in the scanned client files; the
parser behavior is locked with synthetic fixtures.
