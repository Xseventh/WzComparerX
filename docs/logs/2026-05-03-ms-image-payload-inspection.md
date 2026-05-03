# 2026-05-03 MS/MN Image Payload Inspection

## Summary

- Added initial `.ms` / `.mn` image payload extraction for the WC MS-loader
  container family.
- v2/Snow payload extraction now matches WC's `Ms_Image.OpenRead()` flow: one
  continuous Snow pass over the aligned payload plus a second Snow pass over the
  first 1024 bytes.
- v4/ChaCha20 payload extraction matches WC's `Ms_ImageV2.OpenRead()` flow:
  decrypt the first 1024 bytes and read the remaining bytes directly.
- Decrypted payload streams now enter the existing IMG inspection path, so
  `.ms` / `.mn` image entries can populate CLI `inspect`, App IMG Content, and
  existing Canvas/link diagnostics when the IMG payload shape is supported.
- `wcx.package.ms.imageUnsupported` now means a specific image entry could not
  be inspected by the implemented payload extraction or downstream IMG reader,
  not that all MS/MN image extraction is absent.

## Validation

- Deterministic synthetic coverage:
  - WzLib v2/Snow payload decrypt roundtrip.
  - WzLib v4/ChaCha20 payload decrypt roundtrip.
  - Core `InspectAsync` `.ms` v2/v4 image selectors returning a Property tree.
  - CLI `inspect --debug` synthetic `.ms` image output.
  - App ViewModel selecting an `.ms` image node and populating IMG Content.
- Local GMS smoke, with client files kept out of git:

```bash
dotnet src/WzComparerX.Cli/bin/Debug/net10.0/WzComparerX.Cli.dll inspect --debug --key auto "<local-gms-data>/Packs/Skill_00002.ms" "Skill/15500.img"
```

Result summary: `Skill/15500.img [image] : Property`, root `info` and `skill`
objects, Canvas metadata under `info/icon`, and `_outlink` targets resolving to
local `Skill/_Canvas` shards.

## Commands Run

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless
WCX_GMS_DATA_DIR=<local-gms-data> dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~ResourceInspectionGmsSmokeTests
```

## Remaining Work

- Find a real `.mn` client sample to smoke the shared payload path outside
  synthetic fixtures.
- Keep `List.wz` as the next optional-container compatibility slice.
- Expand MS/MN payload coverage only when a real sample exposes an unsupported
  IMG value family or a downstream feature needs it.
