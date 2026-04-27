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

To inspect resources through the stable Core inspection model:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- inspect fixtures/synthetic/basic-tree.json
dotnet run --project src/WzComparerX.Cli --no-build -- inspect --key auto path/to/Base.wz
dotnet run --project src/WzComparerX.Cli --no-build -- inspect --key auto --depth 2 path/to/Base_000.wz StandardPDD.img
dotnet run --project src/WzComparerX.Cli --no-build -- inspect --json fixtures/synthetic/basic-tree.json
```

`inspect` is intended as the shared model surface for future UI, export, search,
and automation workflows. Use `--key auto` to try no-op, KMS, and GMS PKG1
string keys and select the most plausible decoded directory names. Use
`--key kms` or `--key gms` for files that need those legacy PKG1 string keys.
`--key bms` remains accepted as a compatibility alias for WC's historical
no-op key name.

For IMG inspection, the selector can be an image name, image path, or entry
index. `--depth 0` prints only the top-level object type, while larger values
expand nested property/object metadata up to the accepted `0` through `64`
range.

For development diagnostics, add `--debug`:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- inspect --debug --key auto path/to/Base.wz
dotnet run --project src/WzComparerX.Cli --no-build -- inspect --debug --json --key auto --depth 2 path/to/Base_000.wz StandardPDD.img
```

`inspect --debug` exposes structured low-level metadata and diagnostics through
the Core inspection model while normal `inspect` remains compact and stable.
Directory debug metadata includes node types, data sizes, checksums, hash offset
positions, hash offsets, calculated offsets, string key selection, WZ version,
and hash version. IMG debug metadata includes selected entry fields, object
types, object value metadata, property type/kind data, and Canvas/RawData/Video/
Sound payload offsets and lengths.

Canvas metadata includes payload compression kind and expected uncompressed byte
length when the texture format is known. Pixel conversion/export is still a
later step. Lua image entries (`*.lua`) report script length and a short UTF-8
snippet. WC text-format IMG streams are also recognized when their payload
starts with `#Property` or `Root <Property>` and are inspected as bounded
`Property` trees.

To export data through the Core export abstraction:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- export --type metadata fixtures/synthetic/basic-tree.json
dotnet run --project src/WzComparerX.Cli --no-build -- export --type text --key auto path/to/String.wz SomeText.img
dotnet run --project src/WzComparerX.Cli --no-build -- export --type lua --key auto path/to/UI.wz SomeScript.lua
```

`export --type metadata` writes the same stable inspection JSON shape used by
`inspect --debug --json`. `export --type text` currently supports WC text-format
IMG streams. `export --type lua` writes the full decoded Lua script for
supported Lua IMG blocks.

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
