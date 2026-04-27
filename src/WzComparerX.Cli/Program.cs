using WzComparerX.Core;

return await RunAsync(args, Console.Out, Console.Error);

static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
{
    if (args.Length is < 2 or > 3)
    {
        WriteUsage(error);
        return 2;
    }

    var command = args[0];
    var json = false;
    var pathIndex = 1;
    if (args.Length == 3)
    {
        if (!string.Equals(args[1], "--json", StringComparison.OrdinalIgnoreCase))
        {
            WriteUsage(error);
            return 2;
        }

        json = true;
        pathIndex = 2;
    }

    var path = args[pathIndex];
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
}
