# 2026-05-08 GMS Effect String Item Smoke

## Summary

- Added optional Core smoke coverage for a real GMS Effect IMG:
  - `Effect/Effect_000.wz`
  - selector `BasicEff.img`
  - value `scout/back/0/_outlink`
  - expected resolution through `Effect/_Canvas/_Canvas_002.wz`
  - expected Canvas preview pixels from the linked direct-zlib Canvas value.
- Added optional Core smoke coverage for future StringLinker input shape:
  - `String/String_000.wz`
  - selector `Eqp.img`
  - expected `Eqp/Cap`, `Eqp/Weapon`, and `Eqp/Accessory` object structure.
- Added optional Core smoke coverage for future Item/Skill domain input shape:
  - `Item/Item_000.wz`
  - selector `SkillOption.img`
  - expected top-level `skill`, `socket`, and `inc` objects plus representative
    `skillId` / `reqLevel` int32 scalar values.

## Notes

These tests are gated by `WCX_GMS_DATA_DIR` and read local client files only.
They continue the M5 evidence-chain work without adding new parser behavior or
committing client data.

## Verification

- `WCX_GMS_DATA_DIR='/Users/seventh/Library/Application Support/MapleStoryNA/Bottles/maplestory/drive_c/Nexon/Library/maplestory/appdata/Data' dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj -m:1 --filter "FullyQualifiedName~InspectOptionalGmsStringEqpImage_ReadsStringLinkerShape|FullyQualifiedName~InspectOptionalGmsItemSkillOptionImage_ReadsSkillOptionScalars|FullyQualifiedName~InspectOptionalGmsEffectImage_ResolvesCanvasOutlinkIdentity|FullyQualifiedName~CanvasOptionalGmsEffectImage_ResolvesOutlinkPreview"`
