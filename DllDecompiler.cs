using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;

namespace TmodExtractor;

internal static class DllDecompiler
{
    public static bool TryDecompileToProject(string dllPath, string outputDirectory)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);

            var settings = new DecompilerSettings
            {
                ThrowOnAssemblyResolveErrors = false,
                RemoveDeadCode = true,
                RemoveDeadStores = true,
                UseSdkStyleProjectFormat = true,
                UseNestedDirectoriesForNamespaces = true,
            };

            var resolver = new UniversalAssemblyResolver(dllPath, false, null);
            resolver.AddSearchDirectory(@"C:\Program Files (x86)\Steam\steamapps\common\tModLoader");
            resolver.AddSearchDirectory(@"C:\Program Files (x86)\Steam\steamapps\common\tModLoader\Libraries\FNA\1.0.0");
            resolver.AddSearchDirectory(@"C:\Program Files (x86)\Steam\steamapps\common\tModLoader\Libraries\ReLogic\1.0.0");

            var peFile = new PEFile(dllPath);
            var decompiler = new WholeProjectDecompiler(settings, resolver, null, null, null);

            var tempDirectory = Path.Combine(Path.GetTempPath(), "TmodExtractor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                decompiler.DecompileProject(peFile, tempDirectory);

                var assemblyName = peFile.Name;
                var generatedSubfolder = Path.Combine(tempDirectory, assemblyName);

                var sourceDirectory = Directory.Exists(generatedSubfolder)
                    ? generatedSubfolder
                    : tempDirectory;

                MoveDirectoryContents(sourceDirectory, outputDirectory);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void MoveDirectoryContents(string sourceDirectory, string targetDirectory)
    {
        foreach (var sourceFile in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            var targetFileDirectory = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetFileDirectory))
                Directory.CreateDirectory(targetFileDirectory);

            File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }
}