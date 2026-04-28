# 2026-04-29 - IMG Lazy Full Inspection

## Summary

Aligned WCX IMG loading with WC's browser behavior.

- WZ/package/directory trees remain eagerly loaded.
- IMG payloads remain lazy: selecting or manually inspecting an IMG is the
  boundary where IMG parsing happens.
- A selected IMG is now inspected as a complete single-IMG tree by default.
- The Avalonia UI no longer exposes an IMG depth control; the visible workflow
  is path/key/selector plus `Inspect Image`.
- CLI `--depth` remains available as an explicit diagnostics limiter for
  deterministic tests and large-output debugging.

## Notes

This mirrors WC's `Wz_Image.TryExtract()` shape: directory loading creates image
nodes with metadata, and full IMG extraction happens only when the user asks for
that IMG. Canvas/Sound/RawData payload decoding remains separate from IMG tree
inspection.

## Verification

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
dotnet test WzComparerX.slnx --no-build -m:1
```

Results:

- Build passed with 0 warnings and 0 errors.
- Tests passed: App 32, Core 50, WzLib 43; total 125.
