# 2026-04-28 - Inspect Abstraction

## Summary

Introduced the first stable Core inspection model so CLI, future UI, export,
search, and automation workflows do not have to depend directly on inspection DTOs.

## Changes

- Added `ResourceInspectionDocument` and `ResourceInspectionNode`.
- Added `ResourceInspectionService` with projections for:
  - synthetic raw fixtures,
  - WZ directory inspections,
  - WZ IMG inspections.
- Added text and JSON inspection formatters.
- Added CLI `inspect`.
- Kept invalid WZ inputs as failures for `inspect`, matching header automation
  semantics.
- Documented that WCX is still early-stage and should prefer timely interface
  refactors over preserving awkward internal API compatibility.

## Verification

- `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`
- `dotnet test WzComparerX.slnx --no-build -m:1`
- Local smoke:
  - `inspect fixtures/synthetic/basic-tree.json`
  - `inspect --key auto` on local `Data/Base/Base.wz`
  - `inspect --key auto --depth 1` on local `Data/Base/Base_000.wz`
    `StandardPDD.img`

## Notes

`inspect` is the product-facing observation surface. New work should prefer the
inspection model, with `inspect --debug` carrying low-level format details when
they are useful beyond one migration session.
