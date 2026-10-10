namespace TmodExtractor;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length is 0 or > 2)
        {
            PrintUsage();
            return 1;
        }

        var tmodPath = Path.GetFullPath(args[0]);
        var outputPath = args.Length > 1 ? Path.GetFullPath(args[1]) : Directory.GetCurrentDirectory();

        try
        {
            var result = TmodReader.Extract(tmodPath, outputPath);

            Console.WriteLine($"Mod: {result.ModName}");
            Console.WriteLine($"Version: {result.ModVersion}");
            Console.WriteLine($"tModLoader: {result.TmodLoaderVersion}");
            Console.WriteLine($"Files extracted: {result.FileCount}");
            Console.WriteLine($"RawImg converted: {result.RawImgConverted}");

            if (result.RawImgFailed > 0)
                Console.WriteLine($"RawImg failed: {result.RawImgFailed}");

            Console.WriteLine($"DLL decompiled: {result.DllDecompiled}");
            Console.WriteLine($"Output: {result.OutputDirectory}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  TmodExtractor <file.tmod> [output-folder]");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  TmodExtractor MyMod.tmod");
        Console.WriteLine("  TmodExtractor MyMod.tmod ./extracted");
    }
}