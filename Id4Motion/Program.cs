using System.Threading.Tasks.Dataflow;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Starting ID4Motion conversion");

Files fileManager = new();

ExecutionDataflowBlockOptions blockOptions = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
DataflowLinkOptions linkOptions = new() { PropagateCompletion = true };

var splitFiles = new TransformManyBlock<IEnumerable<string>, string>(path => path, blockOptions);

var generateFiles = new ActionBlock<string>(async path =>
{
    var fileName = Path.GetFileNameWithoutExtension(path);
    var fileBytes = await File.ReadAllBytesAsync(path);
    
    // Main data
    var data = fileBytes
        .Take(fileBytes.Length - 257)
        .Where((_, index) => index % 17 != 0)
        .ToArray();
    await fileManager.SaveFile(fileName, "Data", data);
    
    // Signature data
    var signature = fileBytes
        .TakeLast(256)
        .ToArray();
    await fileManager.SaveFile(fileName, "Signature", signature);

    Log.Information("Converted: {FileName}", fileName);
}, blockOptions);

splitFiles.LinkTo(generateFiles, linkOptions);
splitFiles.Post(fileManager.FilePaths);
splitFiles.Complete();
generateFiles.Completion.Wait();

Log.Information("Converted {Count} files", fileManager.FileCount);
Log.Information("Press any key to exit");

Console.ReadKey();