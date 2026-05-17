# 2026-05-17 List.wz Folder Scan Boundary

- Kept `List.wz` as a WC compatibility helper instead of a resource package.
- Folder inspection now skips `List.wz` when scanning `*.wz` files, so older
  client folders do not show the helper as an invalid WZ package.
- Direct `inspect path/to/List.wz` behavior is unchanged and remains the
  observation surface for decoded list entries.

Validation:

- Added a deterministic Core folder inspection test with `Base.wz` plus
  synthetic `List.wz`; the folder tree reports only the real package.
- Full solution build/test passed, plus the App Headless subset.
