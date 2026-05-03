# 2026-05-03 MS/MN Folder Scan

## Summary

- Updated folder inspection to enumerate `.wz`, `.ms`, and `.mn` package files.
- Folder package nodes for MS/MN containers can now be opened from the Avalonia
  Resources tree through the same Core inspection path as direct file open.
- Invalid or unsupported MS/MN containers remain visible as package nodes with
  `wcx.package.ms.directoryUnsupported` diagnostics instead of disappearing.

## Notes

- WC's file-open dialog accepts `*.wz;*.ms;*.mn`; WCX's native picker now uses
  the same package family.
- WC only auto-loads `Packs/*.{ms,mn}` for the KMST1125/Base.wz special path.
  WCX folder inspection is slightly more explicit: if the user opens a folder,
  `.ms` / `.mn` packages in that folder tree are listed as packages.
- MS/MN image payload extraction is still not implemented, so selecting an
  image entry from an MS/MN container reports
  `wcx.package.ms.imageUnsupported`.
