using DumbFox.Utils;
namespace DumbFox;

using System.Buffers.Binary;
using System.Net.Sockets;

public static class SfsRead
{
	public static async Task<(byte flags, byte[] payload)?> ReadPacketAsync(NetworkStream stream, CancellationToken ct = default)
	{
		byte[] hdr = await ReadExactAsync(stream, 3, ct);
		if (hdr.Length == 0) return null;

		byte flags = hdr[0];
		bool compressed = (flags & 0x20) != 0;

		// small size variant: 2-byte length
		ushort len = BinaryPrimitives.ReadUInt16BigEndian(hdr.AsSpan(1, 2));
		byte[] payload = await ReadExactAsync(stream, len, ct);
		
		if (compressed)
			payload = Compression.ZlibDecompress(payload);

		return (flags, payload);
	}

	private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int count, CancellationToken ct)
	{
		byte[] buf = new byte[count];
		int read = 0;
		while (read < count)
		{
			int n = await stream.ReadAsync(buf.AsMemory(read, count - read), ct);
			if (n == 0) return read == 0 ? [] : buf[..read];
			read += n;
		}
		return buf;
	}
}