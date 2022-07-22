using Serilog;

public class Files
{
    public const int Offset = 17;
    public const int Line = 16;
    private const string SaveExt = ".bin";
    private const string JdmDir = "JDM";
    private const string ConDir = "Converted";
    
    public readonly List<string> FilePaths;
    public int FileCount => FilePaths.Count;

    public Files()
    {
        var jdmDir = Directory.CreateDirectory(JdmDir);
        Directory.CreateDirectory(ConDir);
        
        FilePaths = Directory.EnumerateFiles(jdmDir.FullName, "*.jdm", SearchOption.AllDirectories).ToList();

        if (!FilePaths.Any())
        {
            Log.Error("No 'jdm' files detected in the JDM folder");
            Log.Information("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(1);
        }
        else
        {
            Log.Information("Detected {FileCount} files", FileCount);
        }
    }

    public async Task SaveFile(string fileName, string type, byte[] bytes)
    {
        string folder = $"{ConDir}/{fileName}";
        Directory.CreateDirectory(folder);
        string path = $"{folder}/{type}{SaveExt}";
        await File.WriteAllBytesAsync(path, bytes);
    }
}