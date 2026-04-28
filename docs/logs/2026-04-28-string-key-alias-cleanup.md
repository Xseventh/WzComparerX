# 2026-04-28 - String Key Alias Cleanup

## Summary

Removed an ambiguous no-op string-key alias from the current CLI surface.

## Changes

- `--key none` and `--key noop` remain the explicit no-op modes.
- `--key kms`, `--key gms`, and `--key auto` are unchanged.
- Command docs and earlier iteration notes now avoid presenting the no-op mode
  as a service-region key.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
