using WzComparerX.Core;

return await RunAsync(args, Console.Out, Console.Error);

static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
{
    if (args.Length != 2)
    {
        WriteUsage(error);
        return 2;
    }

    var command = args[0];
    var path = args[1];
    if (!File.Exists(path))
    {
        error.WriteLine($"File not found: {path}");
        return 1;
    }

    try
    {
        if (string.Equals(command, "list", StringComparison.OrdinalIgnoreCase))
        {
            var documentService = new ResourceDocumentService();
            var formatter = new ResourceTreeListFormatter();
            var workspace = new ResourceWorkspace();

            var document = await documentService.OpenAsync(path);
            workspace.Add(document);

            output.Write(formatter.Format(document));
            return 0;
        }

        if (string.Equals(command, "header", StringComparison.OrdinalIgnoreCase))
        {
            var headerService = new WzPackageHeaderService();
            var formatter = new WzPackageHeaderFormatter();

            var header = await headerService.ReadAsync(path);
            output.Write(formatter.Format(header));
            return header.IsValid ? 0 : 1;
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
    error.WriteLine("  wcx list <synthetic-fixture.json>");
    error.WriteLine("  wcx header <wz-file>");
}
