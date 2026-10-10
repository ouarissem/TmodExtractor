namespace TmodExtractor;

internal sealed record ExtractionResult(
    string ModName,
    string ModVersion,
    string TmodLoaderVersion,
    int FileCount,
    string OutputDirectory,
    int RawImgConverted,
    int RawImgFailed,
    int DllDecompiled);