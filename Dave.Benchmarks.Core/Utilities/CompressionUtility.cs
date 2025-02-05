using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Dave.Benchmarks.Core.Utilities;

public static class CompressionUtility
{
    public static byte[] CompressText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<byte>();
        }

        var bytes = Encoding.UTF8.GetBytes(text);
        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using (var gs = new GZipStream(mso, CompressionMode.Compress))
        {
            msi.CopyTo(gs);
        }

        return mso.ToArray();
    }

    public static string DecompressToText(byte[] compressed)
    {
        if (compressed == null || compressed.Length == 0)
        {
            return string.Empty;
        }

        using var msi = new MemoryStream(compressed);
        using var mso = new MemoryStream();
        using (var gs = new GZipStream(msi, CompressionMode.Decompress))
        {
            gs.CopyTo(mso);
        }

        return Encoding.UTF8.GetString(mso.ToArray());
    }
}
