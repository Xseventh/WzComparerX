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
    var stringKey = WzStringEncryptionKind.Bms;
    string? path = null;
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

        if (path is not null)
        {
            WriteUsage(error);
            return 2;
        }

        path = args[i];
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
            var previewService = new WzDirectoryPreviewService(stringKey);

            var preview = await previewService.ReadAsync(path);
            output.Write(json
                ? new WzDirectoryPreviewJsonFormatter().Format(preview)
                : new WzDirectoryPreviewFormatter().Format(preview));
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
    error.WriteLine("  wcx preview-dir [--json] [--key bms|kms|gms] <pkg1-wz-file>");
}

static bool TryParseStringKey(string value, out WzStringEncryptionKind kind)
{
    if (string.Equals(value, "bms", StringComparison.OrdinalIgnoreCase))
    {
        kind = WzStringEncryptionKind.Bms;
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

    kind = WzStringEncryptionKind.Bms;
    return false;
}
