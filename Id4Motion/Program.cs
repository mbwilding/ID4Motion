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
    int offset = 17;
    int count = 0;
    bool exit = false;
    List<byte[]> list = new();
    for (;;)
    {
        var remainder = fileBytes.Length - count - 257;
        if (offset >= remainder)
        {
            offset = remainder;
            exit = true;
        }
        byte[] buffer = new byte[offset];
        Buffer.BlockCopy(fileBytes, count, buffer, 0, buffer.Length);
        list.Add(buffer.Skip(1).ToArray());
        
        if (exit) break;
        count += offset;
    }
    await fileManager.SaveFile(fileName, "Data", list.SelectMany(x => x).ToArray());

    // Key data
    var key = fileBytes
        .TakeLast(256)
        .ToArray();
    await fileManager.SaveFile(fileName, "Signature", key);

    Log.Information("Converted: {FileName}", fileName);
}, blockOptions);

splitFiles.LinkTo(generateFiles, linkOptions);
splitFiles.Post(fileManager.FilePaths);
splitFiles.Complete();
generateFiles.Completion.Wait();

Log.Information("Converted {Count} files", fileManager.FileCount);
Log.Information("Press any key to exit");

Console.ReadKey();