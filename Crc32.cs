namespace TmodExtractor;

internal static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    public static uint Compute(byte[] typeBytes, byte[] data)
    {
        var crc = 0xFFFFFFFFu;

        foreach (var b in typeBytes)
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);

        foreach (var b in data)
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);

        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }
}