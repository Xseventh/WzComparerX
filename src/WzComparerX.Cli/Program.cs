using WzComparerX.Core;

return await RunAsync(args, Console.Out, Console.Error);

static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
{
    if (args.Length != 2 || !string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase))
    {
        error.WriteLine("Usage: wcx list <synthetic-fixture.json>");
        return 2;
    }

    var path = args[1];
    if (!File.Exists(path))
    {
        error.WriteLine($"File not found: {path}");
        return 1;
    }

    try
    {
        var documentService = new ResourceDocumentService();
        var formatter = new ResourceTreeListFormatter();
        var workspace = new ResourceWorkspace();

        var document = await documentService.OpenAsync(path);
        workspace.Add(document);

        output.Write(formatter.Format(document));
        return 0;
    }
    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
    {
        error.WriteLine(ex.Message);
        return 1;
    }
}
