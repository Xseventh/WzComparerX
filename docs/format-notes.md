# Format Notes

This file is a working notebook for MapleStory resource format observations.
It should grow alongside fixtures and tests.

## Known Resource Families

- Classic WZ files.
- Extended WZ layouts.
- MS/MN-style files.
- PKG2 variants.
- Text-format IMG files.
- Canvas image payloads.
- Sound payloads.
- Video payloads.
- Spine animation resources.

## Open Questions

- Which minimum fixture set can cover the first useful WZ browser milestone?
- Which WZ versions can be represented with small synthetic files?
- Which cases require real client-derived samples?
- How should sensitive or copyrighted resource fixtures be handled?
- Can fixtures be minimized to metadata-only or generated payloads?

## Fixture Policy

Fixtures should be:

- Small.
- Legal to store in the repository.
- Named by the behavior they cover, not by client version alone.
- Paired with expected output where possible.

If a real client sample is required, keep it outside git and document how to
place it locally.

## Behaviors To Cover First

- File header/version detection.
- Directory enumeration.
- Image node lazy extraction.
- Scalar property parsing.
- Vector parsing.
- UOL/link resolution.
- Canvas metadata.
- PNG decode for one simple format.
- String search.
- XML/JSON dump stability.

## Notes From WC

WC currently supports multiple compatibility paths in `WzComparerR2.WzLib`,
including newer PKG2-related changes and KMST/KMS-specific format changes. WCX
should treat those paths as reference behavior and add tests before reshaping
the code.
