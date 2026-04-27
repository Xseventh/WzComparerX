# Commands

Common commands for WCX development.

## Environment

```bash
dotnet --version
dotnet --list-sdks
rg --version
ffmpeg -version
magick -version
```

Expected current SDK:

```text
10.0.203
```

## Restore

```bash
dotnet restore WzComparerX.slnx
```

## Build

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
```

The `-m:1` and `UseSharedCompilation=false` flags keep MSBuild behavior calmer
inside Codex.

## Test

```bash
dotnet test WzComparerX.slnx --no-build -m:1
```

## Format

```bash
dotnet format WzComparerX.slnx
```

For a check-only run:

```bash
dotnet format WzComparerX.slnx --verify-no-changes
```

## Run CLI

```bash
dotnet run --project src/WzComparerX.Cli -- --help
```

After building, the Milestone 1 fixture browser can be run with:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- list fixtures/synthetic/basic-tree.json
```

For machine-readable output:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- list --json fixtures/synthetic/basic-tree.json
```

To inspect the package header of a WZ-like file:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- header path/to/file.wz
```

Header output also supports JSON:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- header --json path/to/file.wz
```

To scan WZ package headers recursively under a file or directory:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- headers path/to/Data/Base
dotnet run --project src/WzComparerX.Cli --no-build -- headers --json path/to/Data/Base
```

To preview raw PKG1 top-level directory entries without decrypting names or
calculating real offsets:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir path/to/Base.wz
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --json path/to/Base.wz
```

## Run Avalonia App

```bash
dotnet run --project src/WzComparerX.App
```

In Codex, opening GUI apps may require elevated permissions or a user-run
terminal.

## Known Codex Sandbox Notes

MSBuild may fail in the normal sandbox with:

```text
System.Net.Sockets.SocketException (13): Permission denied
```

This is caused by named-pipe/socket restrictions. Rerun build/test with elevated
permissions when needed.

NuGet restore requires network access. If restore fails with DNS or
`api.nuget.org` errors, rerun with elevated permissions.

On macOS inside the Codex sandbox, `dotnet run` may hang during the SDK
build/run step after printing a CSSM warning. If the solution has already been
built, use `dotnet run --no-build` or execute the built CLI DLL directly.
