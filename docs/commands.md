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

To preview raw PKG1 top-level directory entries without recursively loading
child payloads:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir path/to/Base.wz
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --json path/to/Base.wz
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --key auto path/to/Base.wz
dotnet run --project src/WzComparerX.Cli --no-build -- preview-dir --key gms path/to/older-client.wz
```

`preview-dir` defaults to `--key none`, which matches the current local
MapleStoryNA client. Use `--key auto` to try no-op, KMS, and GMS string keys
and select the most plausible decoded directory names. Use `--key kms` or
`--key gms` for files that need those legacy PKG1 string keys. `--key bms`
remains accepted as a compatibility alias for WC's historical no-op key name.

When PKG1 version detection succeeds, `preview-dir` also prints `stringKey`,
`wzVersion`, `hashVersion`, and calculated entry offsets.
If nested directory tables are present, `preview-dir` prints all discovered
entries in linear read order and adds `totalEntries`.

To preview the top-level IMG object type for a PKG1 image entry:

```bash
dotnet run --project src/WzComparerX.Cli --no-build -- preview-img path/to/Base_000.wz smap.img
dotnet run --project src/WzComparerX.Cli --no-build -- preview-img --json path/to/Base_000.wz 1
dotnet run --project src/WzComparerX.Cli --no-build -- preview-img --key auto path/to/Base_000.wz StandardPDD.img
dotnet run --project src/WzComparerX.Cli --no-build -- preview-img --depth 2 path/to/Base_000.wz StandardPDD.img
```

The selector can be an image name, image path, or preview entry index.
`preview-img --key auto` reuses the directory preview key detection before
reading the selected IMG payload.
For top-level `Property` images, `preview-img` also lists the first layer of
property names and simple scalar values. Nested objects are summarized by object
type by default. Use `--depth 0` to print only the top-level object type, or
`--depth 2` to expand one nested `Property` layer. The accepted depth range is
`0` through `64`. IMG object previews currently include `Property`,
`Shape2D#Vector2D`, `Shape2D#Convex2D`, `UOL`, Canvas metadata, and RawData
metadata, Canvas#Video metadata, and Sound_DX8 metadata; Canvas pixel decoding,
RawData payload decoding, video payload decoding, and audio payload decoding are
not implemented yet. When the IMG root object is one of those supported
non-`Property` object types, `preview-img` prints its top-level `objectValue`
metadata directly.

Lua image entries (`*.lua`) are previewed as `objectType: Lua` with block count,
payload length, and a short UTF-8 snippet. Full Lua script export is not
implemented yet.

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
