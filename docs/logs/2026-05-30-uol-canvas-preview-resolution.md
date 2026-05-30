# 2026-05-30 UOL Canvas Preview Resolution

## Summary

- Extended Core Canvas preview selection so UOL values use the shared
  `ResourceInspectionLinkResolver` instead of being treated as unsupported
  non-Canvas values.
- Added WC-style chained preview behavior for UOL frames that resolve to a
  placeholder Canvas with `source`, `_inlink`, or `_outlink` children. Directly
  selected Canvas nodes still preview the exact selected value.
- Allowed Avalonia IMG Content UOL nodes to trigger Preview selection.
- Fixed the App test PKG1 fixture writer to emit compressed image sizes instead
  of a single byte, so larger synthetic IMG payloads remain valid.

## Validation

- Added deterministic Core tests for:
  - WZ UOL -> Canvas -> `_outlink` preview resolution.
  - MS container UOL -> Canvas -> `_outlink` preview resolution.
- Added an App ViewModel test for selecting a UOL node from IMG Content and
  loading the linked Canvas preview.
- Ran:

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1 --filter "CanvasImageService_ResolvesUolThenOutlinkCanvasPixels|CanvasImageService_ResolvesMsUolThenOutlinkCanvasPixels|SelectingUolNode_LoadsLinkedCanvasPreview"
```

## Notes

- WC keeps UOL as a `Wz_Uol` value; it is not converted into `_outlink`.
  Preview-like consumers first resolve the UOL target and then, when the target
  is a Canvas placeholder, follow Canvas link children in WC order:
  `source`, `_inlink`, `_outlink`.
- This supports the common Mob frame shape where `attack5/0` is a UOL pointing
  to `../attack6/0`, and that target frame carries `_outlink` to the actual
  atlas Canvas.
