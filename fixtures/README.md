# Fixtures

This directory is reserved for small test fixtures used by WCX.

Do not commit full game client files here. Prefer synthetic or minimized samples
that cover one behavior at a time.

## Suggested Layout

```text
fixtures/
  synthetic/
  external/
  expected/
```

- `synthetic/`: Generated or hand-built samples that are safe to commit.
- `external/`: Local-only samples copied by the developer. Keep real client
  files out of git.
- `expected/`: Expected JSON/XML/text/stdout/stderr outputs for regression
  tests.

Text `.hex` fixtures under `synthetic/` are byte-for-byte binary samples encoded
as hexadecimal so they stay reviewable in git. Tests materialize them into
temporary files before invoking the CLI.

## Naming

Use behavior-oriented names:

- `basic-directory-tree`
- `lazy-image-extraction`
- `text-img-v1`
- `pkg2-version-detection`
- `canvas-png-format-...`

## Local External Samples

If a test needs a local real sample, document it in a companion `.md` file and
mark the test as explicit/manual unless a legal redistributable sample exists.
