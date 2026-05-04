# 2026-05-05 IMG Scalar Fixtures

## Summary

- Added deterministic WzLib coverage for supported binary IMG scalar property
  encodings:
  - null;
  - int16 tags `0x02` and `0x0b`;
  - compressed and expanded int32;
  - alternate int32 tag `0x13`;
  - compressed and expanded int64;
  - compressed and expanded single;
  - double;
  - string.
- Added coverage for empty nested `Property` object nodes so zero-child
  properties stay visible and unambiguous.
- Added CLI `inspect --debug` coverage for the same representative scalar set
  so the parser behavior is observable through the supported Core inspection
  surface.

## Notes

This iteration does not add new parser behavior. It locks the existing migrated
WC scalar behavior before M5 compare/search/resource-model work starts relying
on scalar value equality and stable debug output.

## Verification

- `dotnet test tests/WzComparerX.WzLib.Tests/WzComparerX.WzLib.Tests.csproj -m:1 --filter "FullyQualifiedName~WzImageInspectionReaderTests.Read_ReturnsSupportedScalarPropertyEncodings|FullyQualifiedName~WzImageInspectionReaderTests.Read_ReturnsEmptyNestedProperty"`
- `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj -m:1 --filter FullyQualifiedName~InspectDebugImage_EmitsScalarPropertyEncodings`
