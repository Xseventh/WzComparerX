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
