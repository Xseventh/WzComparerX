using WzComparerX.Core;
using WzComparerX.WzLib;

return await RunAsync(args, Console.Out, Console.Error);

static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
{
    if (args.Length < 2)
    {
        WriteUsage(error);
        return 2;
    }

    var command = args[0];
    var json = false;
    WzStringEncryptionKind? stringKey = WzStringEncryptionKind.None;
    var imagePropertyDepth = 1;
    string? path = null;
    string? selector = null;
    for (var i = 1; i < args.Length; i++)
    {
        if (string.Equals(args[i], "--json", StringComparison.OrdinalIgnoreCase))
        {
            json = true;
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

        if (string.Equals(args[i], "--depth", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Length ||
                !int.TryParse(args[i + 1], out imagePropertyDepth) ||
                imagePropertyDepth < 0 ||
                imagePropertyDepth > WzImagePreviewReader.MaxPropertyPreviewDepth)
            {
                error.WriteLine($"Depth must be between 0 and {WzImagePreviewReader.MaxPropertyPreviewDepth}.");
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
            (string.Equals(command, "preview-img", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(command, "inspect", StringComparison.OrdinalIgnoreCase)))
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

        if (string.Equals(command, "preview-dir", StringComparison.OrdinalIgnoreCase))
        {
            var preview = stringKey is null
                ? await WzDirectoryPreviewService.ReadAutoAsync(path)
                : await new WzDirectoryPreviewService(stringKey.Value).ReadAsync(path);
            output.Write(json
                ? new WzDirectoryPreviewJsonFormatter().Format(preview)
                : new WzDirectoryPreviewFormatter().Format(preview));
            return preview.Header.IsValid ? 0 : 1;
        }

        if (string.Equals(command, "preview-img", StringComparison.OrdinalIgnoreCase))
        {
            if (selector is null)
            {
                WriteUsage(error);
                return 2;
            }

            var preview = stringKey is null
                ? await WzImagePreviewService.ReadAutoAsync(path, selector, imagePropertyDepth)
                : await new WzImagePreviewService(stringKey.Value, imagePropertyDepth).ReadAsync(path, selector);
            output.Write(json
                ? new WzImagePreviewJsonFormatter().Format(preview)
                : new WzImagePreviewFormatter().Format(preview));
            return preview.IsValid ? 0 : 1;
        }

        if (string.Equals(command, "inspect", StringComparison.OrdinalIgnoreCase))
        {
            var service = new ResourceInspectionService();

            var inspection = await service.InspectAsync(path, selector, stringKey, imagePropertyDepth);
            output.Write(json
                ? new ResourceInspectionJsonFormatter().Format(inspection)
                : new ResourceInspectionFormatter().Format(inspection));
            return 0;
        }

        WriteUsage(error);
        return 2;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
    {
        error.WriteLine(ex.Message);
        return 1;
    }
}

static void WriteUsage(TextWriter error)
{
    error.WriteLine("Usage:");
    error.WriteLine("  wcx list [--json] <synthetic-fixture.json>");
    error.WriteLine("  wcx header [--json] <wz-file>");
    error.WriteLine("  wcx headers [--json] <wz-file-or-directory>");
    error.WriteLine("  wcx inspect [--json] [--key auto|none|kms|gms] [--depth 0-64] <synthetic-json-or-wz-file> [image-name-or-index]");
    error.WriteLine("  wcx preview-dir [--json] [--key auto|none|kms|gms] <pkg1-wz-file>");
    error.WriteLine("  wcx preview-img [--json] [--key auto|none|kms|gms] [--depth 0-64] <pkg1-wz-file> <image-name-or-index>");
}

static bool TryParseStringKey(string value, out WzStringEncryptionKind? kind)
{
    if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
    {
        kind = null;
        return true;
    }

    if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "noop", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "bms", StringComparison.OrdinalIgnoreCase))
    {
        // WC historically names this no-op key BMS; keep that as an alias only.
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
