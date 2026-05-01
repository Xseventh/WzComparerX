# 2026-05-02 Headless Test Convention

## Summary

- Ran the Avalonia Headless test subset after the latest M5 work.
- Recorded the convention that future App/UI or browser-facing iterations
  should prefer Headless validation in addition to the standard build/test path.

## Verification

- Passed: `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless`
  - 13 total, 0 failed.
- Passed: `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1`
  - 55 total, 0 failed.
