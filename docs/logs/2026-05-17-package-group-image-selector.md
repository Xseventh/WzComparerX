# 2026-05-17 Package Group Image Selector

- Aligned manual IMG selection with WC-style package groups.
- Core image inspection now checks the entry package first and then numbered
  shards from the same package group when the selector is not present in the
  entry package.
- Canvas preview and Canvas export now open the true selected package source
  returned by the image loader, so entry package paths such as `Map1.wz` or
  `_Canvas.wz` can resolve IMG payloads stored in `Map1_000.wz` /
  `_Canvas_000.wz...`.
- App `Load IMG` keeps the Resources tree focused on the entry package while
  loading IMG Content from the resolved shard source.

Validation:

- Targeted Core tests passed for package-group image inspect, Canvas preview,
  and Canvas export.
- Targeted App tests passed for selecting merged shard images and manual
  package-group selector loading.
- Full solution build passed:
  `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`.
- Full solution tests passed:
  `dotnet test WzComparerX.slnx --no-build -m:1` (App 62, Core 125, WzLib 74).
- App Headless subset passed:
  `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless` (15 tests).
