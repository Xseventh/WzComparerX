# 2026-05-31 Canvas Service Context Split

## Summary

- Split Canvas image context loading out of `ResourceCanvasImageService` into
  `CanvasImageInspectionContext`.
- Split Canvas target ownership / disposal out into `CanvasImageTarget`.
- Kept selector, link resolution, diagnostics, and decode behavior unchanged.

## Validation

- Ran:

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1 --filter "CanvasImageService|ExportCanvas|SelectingUolNode_LoadsLinkedCanvasPreview"
```

The targeted Canvas export / preview tests passed, including the App UOL
selection smoke and Core Canvas export / preview paths.

## Notes

- This is an architecture cleanup after aligning CLI Canvas export with
  Avalonia Preview. The next split should target Canvas target selection/link
  resolution if the service grows again.
