# 2026-05-17 UI RawData Smoke

## Context

M5's media metadata row tracks Sound / Video / RawData as parser metadata that
should stay observable through `inspect --debug` while actual payload decoding
remains a later export/playback feature. Sound already had local GMS smoke, but
RawData was still only covered by synthetic fixtures.

## Changes

- Added optional external-client Core smoke for `Data/UI/UI_000.wz`, selector
  `Login.img`.
- The smoke validates `ClassSelect/back/1/110/skeleton.skel` as a real
  `RawData` value with version `1`, data length `58688`, data offset
  `15358714`, stable identity value path, and the existing
  `wcx.payload.rawData.unsupported` info diagnostic.
- Updated parser coverage and handoff docs to record RawData real-client
  metadata coverage while keeping RawData payload decoding out of scope.

## Validation

Run the targeted smoke with a local client `Data` directory:

```bash
WCX_CLIENT_DATA_DIR=/path/to/Data dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~InspectOptionalExternalClientUiImage_ReadsRawDataMetadata
```

The targeted smoke passed against the local GMS client. Full solution build/test
and the standard App Headless subset also passed before commit.
