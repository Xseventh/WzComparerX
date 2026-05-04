# 2026-05-05 GMS IMG Smoke Expansion

## Summary

- Added optional Core smoke coverage for a real GMS Character IMG:
  - `Character/Character_000.wz`
  - selector `00002000.img`
  - value `walk1/0/body/_outlink`
  - expected resolution through `Character/_Canvas/_Canvas_000.wz`
  - expected Canvas preview pixels from the linked direct-zlib Canvas value.
- Added optional Core smoke coverage for a real GMS Sound IMG:
  - `Sound/Sound_000.wz`
  - selector `AchievementEff.img`
  - expected `GradeUp` Sound_DX8 metadata including duration, data offset, and
    data length.
  - expected stable `wcx.payload.audio.unsupported` diagnostic while audio
    decode/export remains out of scope.

## Notes

These tests are gated by `WCX_GMS_DATA_DIR` and read local client files only.
They do not commit client data. The goal is to strengthen the M5 evidence chain
for deeper IMG value families before Compare/Search/resource-model work starts
depending on link identity, Canvas preview, and media payload diagnostics.

## Verification

- `WCX_GMS_DATA_DIR='/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data' dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj -m:1 --filter "FullyQualifiedName~InspectOptionalGmsCharacterImage_ResolvesCanvasOutlinkIdentity|FullyQualifiedName~CanvasOptionalGmsCharacterImage_ResolvesOutlinkPreview|FullyQualifiedName~InspectOptionalGmsSoundImage_ReadsSoundPayloadMetadata"`
