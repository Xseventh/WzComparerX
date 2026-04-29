# 2026-04-29 Inspection Default Cleanup

## Summary

Cleaned up two small M4 follow-through items after the WC-style IMG browser
work.

## Changes

- Updated the `ResourceInspectionService.InspectAsync` convenience overload so
  its default max property depth matches `ResourceInspectionOptions` and the
  current full IMG inspection direction.
- Added a Core regression test that uses the convenience overload and verifies
  nested IMG properties are projected by default.
- Updated `docs/handoff.md` to describe App key/options parsing instead of the
  removed UI depth field.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test tests/WzComparerX.Core.Tests/WzComparerX.Core.Tests.csproj --no-build --filter InspectImageConvenienceOverload_UsesFullPropertyDepthByDefault`
- `dotnet test WzComparerX.slnx --no-build -m:1`
