# 2026-05-17 Linked Image Identity Guard

## Context

The optional parser scan for real IMG value samples exposed an easy tooling
mistake: image nodes projected from linked or merged package trees must be
loaded through their own `Identity.PackagePath`, not through the entry package
that happened to display them.

## Changes

- Tightened the deterministic Base-style split-package test to assert linked
  image nodes preserve the true source package and selector identity.
- Refreshed the handoff recent commit list.

## Validation

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless
```
