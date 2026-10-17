using System.IO.Compression;

namespace DumbFox.Utils;

public static class Compression
{
	public static byte[] ZlibCompress(ReadOnlySpan<byte> input)
	{
		using var ms = new MemoryStream();
		using (var zs = new ZLibStream(ms, CompressionLevel.Fastest, leaveOpen: true))
			zs.Write(input);
		return ms.ToArray();
	}

	public static byte[] ZlibDecompress(ReadOnlySpan<byte> input)
	{
		using var src = new MemoryStream(input.ToArray());
		using var zs = new ZLibStream(src, CompressionMode.Decompress);
		using var dst = new MemoryStream();
		zs.CopyTo(dst);
		return dst.ToArray();
	}
}
