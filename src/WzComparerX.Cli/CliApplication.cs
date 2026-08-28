using WzComparerX.Core;
using WzComparerX.Rendering;
using WzComparerX.WzLib;

namespace WzComparerX.Cli;

public static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2)
        {
            WriteUsage(error);
            return 2;
        }

        var command = args[0];
        var json = false;
        var debug = false;
        var exportKind = ResourceExportKind.Metadata;
        WzStringEncryptionKind? stringKey = WzStringEncryptionKind.None;
        var imagePropertyDepth = WzImageInspectionReader.FullPropertyInspectionDepth;
        string? path = null;
        string? selector = null;
        string? exportOutputPath = null;
        string? exportValueSelector = null;
        for (var i = 1; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--json", StringComparison.OrdinalIgnoreCase))
            {
                json = true;
                continue;
            }

            if (string.Equals(args[i], "--out", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    WriteUsage(error);
                    return 2;
                }

                exportOutputPath = args[i + 1];
                i++;
                continue;
            }

            if (string.Equals(args[i], "--value", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    WriteUsage(error);
                    return 2;
                }

                exportValueSelector = args[i + 1];
                i++;
                continue;
            }

            if (string.Equals(args[i], "--debug", StringComparison.OrdinalIgnoreCase))
            {
                debug = true;
                continue;
            }

            if (string.Equals(args[i], "--key", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || !TryParseStringKey(args[i + 1], out stringKey))
                {
                    WriteUsage(error);
                    return 2;
                }

                i++;
                continue;
            }

            if (string.Equals(args[i], "--type", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || !TryParseExportKind(args[i + 1], out exportKind))
                {
                    WriteUsage(error);
                    return 2;
                }

                i++;
                continue;
            }

            if (string.Equals(args[i], "--depth", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length ||
                    !int.TryParse(args[i + 1], out imagePropertyDepth) ||
                    imagePropertyDepth < 0 ||
                    imagePropertyDepth > WzImageInspectionReader.MaxPropertyInspectionDepth)
                {
                    error.WriteLine($"Depth must be between 0 and {WzImageInspectionReader.MaxPropertyInspectionDepth}.");
                    WriteUsage(error);
                    return 2;
                }

                i++;
                continue;
            }

            if (path is null)
            {
                path = args[i];
                continue;
            }

            if (selector is null &&
                (string.Equals(command, "inspect", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(command, "export", StringComparison.OrdinalIgnoreCase)))
            {
                selector = args[i];
                continue;
            }

            WriteUsage(error);
            return 2;
        }

        if (path is null)
        {
            WriteUsage(error);
            return 2;
        }

        if (exportValueSelector is not null && !string.Equals(command, "export", StringComparison.OrdinalIgnoreCase))
        {
            WriteUsage(error);
            return 2;
        }

        if (json && string.Equals(command, "export", StringComparison.OrdinalIgnoreCase))
        {
            error.WriteLine("--json is not supported for export because export writes resource content directly.");
            return 2;
        }

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            error.WriteLine($"File or directory not found: {path}");
            return 1;
        }

        try
        {
            if (string.Equals(command, "list", StringComparison.OrdinalIgnoreCase))
            {
                var documentService = new ResourceDocumentService();
                var workspace = new ResourceWorkspace();

                var document = await documentService.OpenAsync(path);
                workspace.Add(document);

                output.Write(json
                    ? new ResourceTreeJsonFormatter().Format(document)
                    : new ResourceTreeListFormatter().Format(document));
                return 0;
            }

            if (string.Equals(command, "header", StringComparison.OrdinalIgnoreCase))
            {
                var headerService = new WzPackageHeaderService();

                var header = await headerService.ReadAsync(path);
                output.Write(json
                    ? new WzPackageHeaderJsonFormatter().Format(header)
                    : new WzPackageHeaderFormatter().Format(header));
                return header.IsValid ? 0 : 1;
            }

            if (string.Equals(command, "headers", StringComparison.OrdinalIgnoreCase))
            {
                var scanService = new WzPackageHeaderScanService();

                var headers = await scanService.ScanAsync(path);
                output.Write(json
                    ? new WzPackageHeaderScanJsonFormatter().Format(headers)
                    : new WzPackageHeaderScanFormatter().Format(headers));
                return headers.All(header => header.IsValid) ? 0 : 1;
            }

            if (string.Equals(command, "inspect", StringComparison.OrdinalIgnoreCase))
            {
                var service = new ResourceInspectionService();

                var options = new ResourceInspectionOptions(stringKey, imagePropertyDepth, debug);
                var inspection = await service.InspectAsync(path, selector, options);
                output.Write(json
                    ? new ResourceInspectionJsonFormatter().Format(inspection)
                    : new ResourceInspectionFormatter().Format(inspection));
                return HasErrorDiagnostics(inspection) ? 1 : 0;
            }

            if (string.Equals(command, "export", StringComparison.OrdinalIgnoreCase))
            {
                if (exportKind == ResourceExportKind.Video)
                {
                    return await ExportVideoAsync(
                        path,
                        selector,
                        exportOutputPath,
                        exportValueSelector,
                        stringKey,
                        imagePropertyDepth,
                        error);
                }

                if (exportKind == ResourceExportKind.Spine)
                {
                    return await ExportSpineAsync(
                        path,
                        selector,
                        exportOutputPath,
                        exportValueSelector,
                        stringKey,
                        imagePropertyDepth,
                        error);
                }

                var service = new ResourceExportService();

                var options = new ResourceExportOptions(exportKind, stringKey, imagePropertyDepth, exportValueSelector);
                var document = await service.ExportAsync(path, selector, options);
                if (exportOutputPath is not null)
                {
                    await File.WriteAllBytesAsync(exportOutputPath, document.Content);
                    WriteDiagnostics(document.Diagnostics, error);
                    return 0;
                }

                if (!document.IsText)
                {
                    WriteDiagnostics([ResourceInspectionDiagnostics.ExportBinaryOutRequired()], error);
                    return 2;
                }

                output.Write(document.GetTextContent());
                WriteDiagnostics(document.Diagnostics, error);
                return 0;
            }

            WriteUsage(error);
            return 2;
        }
        catch (ResourceExportException ex)
        {
            WriteDiagnostics([ex.Diagnostic], error);
            return 1;
        }
        catch (ResourceInspectionException ex)
        {
            WriteDiagnostics([ex.Diagnostic], error);
            return 1;
        }
        catch (ResourceVideoTargetException ex)
        {
            WriteDiagnostics([ex.Diagnostic], error);
            return 1;
        }
        catch (ResourceVideoSequenceException ex)
        {
            WriteVideoDiagnostic(ex.Diagnostic, error);
            return 1;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void WriteUsage(TextWriter error)
    {
        error.WriteLine("Usage:");
        error.WriteLine("  wcx list [--json] <synthetic-fixture.json>");
        error.WriteLine("  wcx header [--json] <wz-file>");
        error.WriteLine("  wcx headers [--json] <wz-file-or-directory>");
        error.WriteLine("  wcx inspect [--json] [--debug] [--key auto|none|noop|kms|gms] [--depth 0-64] <synthetic-json-or-wz-file> [image-name-or-index]");
        error.WriteLine("  wcx export [--type metadata|text|lua|canvas|video|spine] [--out <path-or-directory>] [--value <property-path>] [--key auto|none|noop|kms|gms] [--depth 0-64] <synthetic-json-or-wz-file> [image-name-or-index]");
    }

    private static async Task<int> ExportSpineAsync(
        string path,
        string? selector,
        string? exportOutputPath,
        string? valueSelector,
        WzStringEncryptionKind? stringKey,
        int imagePropertyDepth,
        TextWriter error)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            error.WriteLine("Spine export requires an image selector.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(valueSelector))
        {
            error.WriteLine("Spine export requires --value <property-path>.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(exportOutputPath))
        {
            error.WriteLine("Spine export requires --out <directory>.");
            return 2;
        }

        if (File.Exists(exportOutputPath))
        {
            error.WriteLine($"Spine export output must be a directory, but a file already exists: {exportOutputPath}");
            return 2;
        }

        var service = new ResourceSpineExportService();
        var document = await service.LoadAsync(
            path,
            selector,
            valueSelector,
            new ResourceInspectionOptions(stringKey, imagePropertyDepth));

        var outputRoot = Path.GetFullPath(exportOutputPath);
        Directory.CreateDirectory(outputRoot);
        foreach (var file in document.Files)
        {
            var outputPath = Path.GetFullPath(
                Path.Combine(outputRoot, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsWithinDirectory(outputRoot, outputPath))
            {
                throw new InvalidDataException($"Spine export path escapes the output directory: {file.RelativePath}.");
            }

            var parent = Path.GetDirectoryName(outputPath);
            if (parent is not null)
            {
                Directory.CreateDirectory(parent);
            }

            await File.WriteAllBytesAsync(outputPath, file.Content);
        }

        return 0;
    }

    private static bool IsWithinDirectory(string directory, string path)
    {
        var root = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return path.StartsWith(root, comparison);
    }

    private static async Task<int> ExportVideoAsync(
        string path,
        string? selector,
        string? exportOutputPath,
        string? valueSelector,
        WzStringEncryptionKind? stringKey,
        int imagePropertyDepth,
        TextWriter error)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            error.WriteLine("Video export requires an image selector.");
            return 2;
        }

        if (string.IsNullOrWhiteSpace(exportOutputPath))
        {
            error.WriteLine("Video export requires --out <directory>.");
            return 2;
        }

        if (File.Exists(exportOutputPath))
        {
            error.WriteLine($"Video export output must be a directory, but a file already exists: {exportOutputPath}");
            return 2;
        }

        var service = new ResourceVideoSequenceService();
        var document = await service.LoadAsync(
            path,
            selector,
            valueSelector,
            new ResourceInspectionOptions(stringKey, imagePropertyDepth),
            WzVideoDecodeOptions.Default);

        Directory.CreateDirectory(exportOutputPath);
        var frames = new List<object>(document.Frames.Count);
        for (var i = 0; i < document.Frames.Count; i++)
        {
            var frame = document.Frames[i];
            var fileName = $"frame-{i:D4}.bgra";
            await File.WriteAllBytesAsync(Path.Combine(exportOutputPath, fileName), frame.Pixels);
            frames.Add(new
            {
                index = frame.FrameIndex,
                file = fileName,
                width = frame.Width,
                height = frame.Height,
                stride = frame.Stride,
                pixelFormat = frame.PixelFormat.ToString(),
                startTimeNanoseconds = frame.StartTimeInNanoseconds,
                delayNanoseconds = frame.DelayInNanoseconds
            });
        }

        var manifest = new
        {
            source = document.SourcePath,
            selector = document.Selector,
            value = document.ValuePath,
            fourCc = document.FourCc,
            width = document.Width,
            height = document.Height,
            pixelFormat = document.PixelFormat.ToString(),
            frameCount = document.FrameCount,
            durationNanoseconds = document.DurationInNanoseconds,
            frames
        };
        var json = System.Text.Json.JsonSerializer.Serialize(
            manifest,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(exportOutputPath, "manifest.json"), json);
        return 0;
    }

    private static void WriteDiagnostics(
        IReadOnlyList<ResourceInspectionDiagnostic>? diagnostics,
        TextWriter error)
    {
        if (diagnostics is null)
        {
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            error.WriteLine(ResourceInspectionDiagnosticFormatter.Format(diagnostic));
        }
    }

    private static void WriteVideoDiagnostic(WzVideoDecodeDiagnostic diagnostic, TextWriter error)
    {
        error.WriteLine($"error [{diagnostic.Code}]: {diagnostic.Message}");
    }

    private static bool HasErrorDiagnostics(ResourceInspectionDocument document)
    {
        return HasErrorDiagnostics(document.Diagnostics) || HasErrorDiagnostics(document.Root);
    }

    private static bool HasErrorDiagnostics(ResourceInspectionNode node)
    {
        return HasErrorDiagnostics(node.Diagnostics) || node.Children.Any(HasErrorDiagnostics);
    }

    private static bool HasErrorDiagnostics(IReadOnlyList<ResourceInspectionDiagnostic>? diagnostics)
    {
        return diagnostics?.Any(diagnostic =>
            string.Equals(diagnostic.Severity, ResourceDiagnosticSeverities.Error, StringComparison.Ordinal)) == true;
    }

    private static bool TryParseStringKey(string value, out WzStringEncryptionKind? kind)
    {
        if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
        {
            kind = null;
            return true;
        }

        if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "noop", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.None;
            return true;
        }

        if (string.Equals(value, "kms", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.Kms;
            return true;
        }

        if (string.Equals(value, "gms", StringComparison.OrdinalIgnoreCase))
        {
            kind = WzStringEncryptionKind.Gms;
            return true;
        }

        kind = WzStringEncryptionKind.None;
        return false;
    }

    private static bool TryParseExportKind(string value, out ResourceExportKind kind)
    {
        if (string.Equals(value, "metadata", StringComparison.OrdinalIgnoreCase))
        {
            kind = ResourceExportKind.Metadata;
            return true;
        }

        if (string.Equals(value, "text", StringComparison.OrdinalIgnoreCase))
        {
            kind = ResourceExportKind.Text;
            return true;
        }

        if (string.Equals(value, "lua", StringComparison.OrdinalIgnoreCase))
        {
            kind = ResourceExportKind.Lua;
            return true;
        }

        if (string.Equals(value, "canvas", StringComparison.OrdinalIgnoreCase))
        {
            kind = ResourceExportKind.Canvas;
            return true;
        }

        if (string.Equals(value, "video", StringComparison.OrdinalIgnoreCase))
        {
            kind = ResourceExportKind.Video;
            return true;
        }

        if (string.Equals(value, "spine", StringComparison.OrdinalIgnoreCase))
        {
            kind = ResourceExportKind.Spine;
            return true;
        }

        kind = ResourceExportKind.Metadata;
        return false;
    }
}
