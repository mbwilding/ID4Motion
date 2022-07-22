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
    
    int offset = Files.Offset;
    int count = 0;
    bool exit = false;
    List<byte[]> list = new();
    for (;;)
    {
        var remainder = fileBytes.Length - count;
        if (offset >= remainder)
        {
            offset = remainder;
            exit = true;
        }
        byte[] buffer = new byte[offset];
        Buffer.BlockCopy(fileBytes, count, buffer, 0, buffer.Length);
        list.Add(buffer);
        
        if (exit) break;
        count += offset;
    }

    // Key data
    var keyDelimiterIndex = list.FindIndex(x => x.First() == 0x0);
    var start = keyDelimiterIndex * Files.Offset + 1;
    var last = fileBytes.Length - start;
    var key = fileBytes
        .Skip(start)
        .Take(last)
        .ToArray();
    await fileManager.SaveFile(fileName, "Key", key);
    
    // Main data
    var data = list
        .Take(keyDelimiterIndex)
        .SelectMany(x => x.Skip(1).Take(Files.Line))
        .ToArray(); 
    await fileManager.SaveFile(fileName, "Data", data);



    Log.Information("Converted: {FileName}", fileName);
}, blockOptions);

splitFiles.LinkTo(generateFiles, linkOptions);
splitFiles.Post(fileManager.FilePaths);
splitFiles.Complete();
generateFiles.Completion.Wait();

Log.Information("Converted {Count} files", fileManager.FileCount);
Log.Information("Press any key to exit");

Console.ReadKey();