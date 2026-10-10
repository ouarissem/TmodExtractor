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
            decompiler.DecompileProject(peFile, outputDirectory);

            return true;
        }
        catch
        {
            return false;
        }
    }
}