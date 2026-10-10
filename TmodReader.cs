using System.IO.Compression;
using System.Text;

namespace TmodExtractor;

internal static class TmodReader
{
    private const int HeaderLength = 4;
    private const int HashLength = 20;
    private const int SignatureLength = 256;

    public static ExtractionResult Extract(string tmodPath, string outputPath)
    {
        if (!File.Exists(tmodPath))
            throw new FileNotFoundException($"File not found: {tmodPath}", tmodPath);

        using var fileStream = File.OpenRead(tmodPath);
        using var reader = new BinaryReader(fileStream, Encoding.UTF8, leaveOpen: true);

        var header = Encoding.ASCII.GetString(reader.ReadBytes(HeaderLength));
        if (header != "TMOD")
            throw new InvalidDataException($"Invalid TMOD header: '{header}'");

        var tmodLoaderVersion = reader.ReadString();

        reader.ReadBytes(HashLength);
        reader.ReadBytes(SignatureLength);

        reader.ReadUInt32();

        var modName = reader.ReadString();
        var modVersion = reader.ReadString();
        var fileCount = reader.ReadInt32();

        var fileEntries = new FileEntry[fileCount];
        long dataOffset = 0;

        for (var i = 0; i < fileCount; i++)
        {
            var name = reader.ReadString();
            var uncompressedLength = reader.ReadInt32();
            var compressedLength = reader.ReadInt32();

            fileEntries[i] = new FileEntry(name, uncompressedLength, compressedLength, dataOffset);
            dataOffset += compressedLength;
        }

        var fileDataStart = fileStream.Position;

        var targetDirectory = Path.Combine(outputPath, SanitizeDirectoryName(modName));
        Directory.CreateDirectory(targetDirectory);

        var rawImgConverted = 0;
        var rawImgFailed = 0;
        var dllDecompiled = 0;

        foreach (var entry in fileEntries)
        {
            var (success, isRawImg, isDll) = ExtractFile(entry, fileStream, fileDataStart, targetDirectory);

            if (isRawImg)
            {
                if (success) rawImgConverted++;
                else rawImgFailed++;
            }

            if (isDll && success)
                dllDecompiled++;
        }

        return new ExtractionResult(
            modName,
            modVersion,
            tmodLoaderVersion,
            fileCount,
            targetDirectory,
            rawImgConverted,
            rawImgFailed,
            dllDecompiled);
    }

    private static (bool Success, bool IsRawImg, bool IsDllDecompiled) ExtractFile(
        FileEntry entry,
        Stream sourceStream,
        long fileDataStart,
        string outputDirectory)
    {
        var targetPath = Path.Combine(outputDirectory, entry.Name);
        var targetDir = Path.GetDirectoryName(targetPath);

        if (!string.IsNullOrEmpty(targetDir))
            Directory.CreateDirectory(targetDir);

        sourceStream.Seek(fileDataStart + entry.Offset, SeekOrigin.Begin);

        var rawData = ReadExactly(sourceStream, entry.CompressedLength);

        byte[] finalData;

        if (entry.CompressedLength == entry.UncompressedLength)
        {
            finalData = rawData;
        }
        else
        {
            using var compressedStream = new MemoryStream(rawData);
            using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            deflateStream.CopyTo(outputStream);
            finalData = outputStream.ToArray();
        }

        var isRawImg = entry.Name.EndsWith(".rawimg", StringComparison.OrdinalIgnoreCase);
        var isDll = entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

        // Determine if the DLL is in the root of the archive (the mod's own assembly)
        // or inside a subfolder such as "lib/" (a third-party dependency).
        var isRootDll = isDll && !entry.Name.Contains('/') && !entry.Name.Contains('\\');

        if (isRawImg)
        {
            var pngPath = Path.ChangeExtension(targetPath, ".png");
            var success = RawImageConverter.TryConvertToPng(finalData, pngPath);

            if (success)
                File.Delete(targetPath);
            else
                File.WriteAllBytes(targetPath, finalData);

            return (success, true, false);
        }

        File.WriteAllBytes(targetPath, finalData);

        if (isRootDll)
        {
            var decompiledDirectory = Path.Combine(outputDirectory, "DecompiledSource");
            var decompileSuccess = DllDecompiler.TryDecompileToProject(targetPath, decompiledDirectory);
            return (decompileSuccess, false, true);
        }

        // Dependency DLLs under lib/ or similar folders are extracted as-is, not decompiled.
        return (false, false, false);
    }

    private static byte[] ReadExactly(Stream stream, long length)
    {
        var buffer = new byte[length];
        var totalRead = 0;

        while (totalRead < length)
        {
            var bytesRead = stream.Read(buffer, totalRead, (int)(length - totalRead));
            if (bytesRead == 0)
                throw new EndOfStreamException($"Expected {length} bytes, got {totalRead}");
            totalRead += bytesRead;
        }

        return buffer;
    }

    private static string SanitizeDirectoryName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name.Length);

        foreach (var c in name)
            sanitized.Append(invalidChars.Contains(c) ? '_' : c);

        return sanitized.ToString();
    }
}