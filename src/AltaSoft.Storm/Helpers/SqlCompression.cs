using System.IO;
using System.IO.Compression;
using System.Text;

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// Provides methods for compressing and decompressing SQL data.
/// </summary>
public static class SqlCompression
{
    /// <summary>
    /// Decompresses the specified compressed data.
    /// </summary>
    /// <param name="compressedData">The compressed data to decompress.</param>
    /// <returns>The decompressed string.</returns>
    public static string Decompress(byte[] compressedData)
    {
        using var compressedStream = new MemoryStream(compressedData);
        using var decompressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: true))
        {
            gzipStream.CopyTo(decompressedStream);
        }
        return Encoding.UTF8.GetString(decompressedStream.ToArray());
    }

    /// <summary>
    /// Compresses the specified data.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>The compressed byte array.</returns>
    public static byte[] Compress(string data)
    {
        var byteArray = Encoding.UTF8.GetBytes(data);

        using var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress, leaveOpen: true))
        {
            gzipStream.Write(byteArray, 0, byteArray.Length);
        }

        return compressedStream.ToArray();
    }
}
