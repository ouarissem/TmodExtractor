namespace TmodExtractor;

internal sealed record FileEntry(
    string Name,
    long UncompressedLength,
    long CompressedLength,
    long Offset);