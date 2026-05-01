# 2026-05-02 GMS Container Inventory

## Summary

- Ran a read-only local inventory against the user's GMS `Data` directory.
- This does not commit or copy client files.
- The scan informs M5 optional container and PKG2 priorities.

## Results

- WZ files: 780.
- `headers` scan: no `PKG2`, `unknown`, or invalid WZ headers were reported.
- `List.wz`: not found.
- `.mn`: 0 files.
- `.ms`: 10 files under `Data/Packs`.
  - Observed categories include Mob and Skill packs.

## Implications

- PKG2 remains an important WC compatibility item, but the current local GMS
  client does not provide a direct PKG2 WZ sample.
- `List.wz` and `.mn` remain compatibility targets for older clients even
  though this local GMS install does not provide samples. Their first WCX slice
  should be driven by WC reference behavior or an older-client sample.
- `.ms` is now sample-driven for the current local client. The next parser
  slice should review WC `Ms_File` / `Ms_FileV2` and define the smallest
  inspection behavior before adding code.

## Commands

```bash
find "<local GMS Data directory>" -type f -iname "*.wz" | wc -l
find "<local GMS Data directory>" -type f -iname "*.ms" | wc -l
find "<local GMS Data directory>" -type f -iname "*.mn" | wc -l
find "<local GMS Data directory>" -type f -iname "List.wz" | wc -l
dotnet run --project src/WzComparerX.Cli --no-build -- headers "<local GMS Data directory>" | rg -i "pkg2|unknown|invalid"
```
