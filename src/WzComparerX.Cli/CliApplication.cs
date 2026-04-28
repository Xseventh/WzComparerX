using WzComparerX.Core;
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
        var imagePropertyDepth = 1;
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
                return 0;
            }

            if (string.Equals(command, "export", StringComparison.OrdinalIgnoreCase))
            {
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
        error.WriteLine("  wcx export [--type metadata|text|lua|canvas] [--out <path>] [--value <property-path>] [--key auto|none|noop|kms|gms] [--depth 0-64] <synthetic-json-or-wz-file> [image-name-or-index]");
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

        kind = ResourceExportKind.Metadata;
        return false;
    }
}
