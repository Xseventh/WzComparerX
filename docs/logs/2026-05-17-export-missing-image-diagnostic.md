# 2026-05-17 Export Missing Image Diagnostic

## Context

The stable `wcx.inspection.image.notFound` diagnostic was already covered for
`inspect`, but metadata export also accepts an image selector and should not
fall back to raw exception text when that selector is missing.

## Changes

- Added a CLI golden stderr test for:
  - `export --type metadata --key none <wz> Missing.img`
- Reused the shared image-not-found diagnostic path so export and inspect report
  the same code, source, message, and selector path.

## Notes

- No parser behavior changed.
- This locks a user-visible automation path before Compare/Search/export build
  more workflows on top of resource identity.
