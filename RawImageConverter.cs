using System.Buffers.Binary;
using System.IO.Compression;

namespace TmodExtractor;

internal static class RawImageConverter
{
    private const int RawImgVersion = 1;
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static bool TryConvertToPng(byte[] rawImgData, string outputPngPath)
    {
        try
        {
            var (width, height, pixelData) = ParseRawImg(rawImgData);
            var pngBytes = BuildPng(width, height, pixelData);
            File.WriteAllBytes(outputPngPath, pngBytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static (int Width, int Height, byte[] Pixels) ParseRawImg(byte[] data)
    {
        if (data.Length < 12)
            throw new InvalidDataException("RawImg data too short.");

        var version = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(0, 4));
        if (version != RawImgVersion)
            throw new InvalidDataException($"Unsupported RawImg version: {version}.");

        var width = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4, 4));
        var height = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8, 4));

        if (width <= 0 || height <= 0)
            throw new InvalidDataException($"Invalid dimensions: {width}x{height}.");

        var expectedPixelBytes = width * height * 4;
        if (data.Length < 12 + expectedPixelBytes)
            throw new InvalidDataException("RawImg pixel data incomplete.");

        var pixels = new byte[expectedPixelBytes];
        Buffer.BlockCopy(data, 12, pixels, 0, expectedPixelBytes);

        return (width, height, pixels);
    }

    private static byte[] BuildPng(int width, int height, byte[] pixels)
    {
        using var output = new MemoryStream();
        output.Write(PngSignature);

        WriteChunk(output, "IHDR", BuildIhdr(width, height));

        var scanlines = BuildScanlines(width, height, pixels);
        var compressedScanlines = CompressWithZlib(scanlines);
        WriteChunk(output, "IDAT", compressedScanlines);

        WriteChunk(output, "IEND", Array.Empty<byte>());

        return output.ToArray();
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4, 4), height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        return ihdr;
    }

    private static byte[] BuildScanlines(int width, int height, byte[] pixels)
    {
        var scanlines = new byte[height * (1 + width * 4)];
        var sourceOffset = 0;
        var targetOffset = 0;

        for (var y = 0; y < height; y++)
        {
            scanlines[targetOffset++] = 0;
            Buffer.BlockCopy(pixels, sourceOffset, scanlines, targetOffset, width * 4);
            sourceOffset += width * 4;
            targetOffset += width * 4;
        }

        return scanlines;
    }

    private static byte[] CompressWithZlib(byte[] rawData)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            zlib.Write(rawData, 0, rawData.Length);
        }
        return compressed.ToArray();
    }

    private static void WriteChunk(Stream stream, string chunkType, byte[] data)
    {
        Span<byte> lengthBuffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, data.Length);
        stream.Write(lengthBuffer);

        var typeBytes = System.Text.Encoding.ASCII.GetBytes(chunkType);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = Crc32.Compute(typeBytes, data);
        Span<byte> crcBuffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBuffer, crc);
        stream.Write(crcBuffer);
    }
}