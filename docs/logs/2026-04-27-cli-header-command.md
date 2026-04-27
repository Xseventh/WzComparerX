# 2026-04-27 CLI Header Command

## Summary

Connected the migrated WZ package header reader to Core and CLI workflows.

## Completed

- Added Core header service and deterministic text formatter.
- Added CLI command:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- header path/to/file.wz
```

- Added Core tests for reading and formatting a temporary generated PKG1
  header file.
- Documented the command in `docs/commands.md`.

## Verification

```bash
/usr/local/share/dotnet/dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
/usr/local/share/dotnet/dotnet test WzComparerX.slnx --no-build -m:1
```

## Next Suggested Work

Add a more general `inspect` command shape, likely with `--format text|json`,
then reuse it for both synthetic resource nodes and real WZ header metadata.
