using System.Buffers.Binary;
using System.Net.Sockets;
using System.Net.WebSockets;
using Common.Utils;
using DumbFox.Utils;

namespace DumbFox;

public static class SfsRead
{
	public static Task<(byte flags, byte[] payload)?> ReadPacketAsync(Session session, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(session);

		if (session.Socket is not null)
			return ReadPacketAsync(session.Socket, ct);

		if (session.Stream is not null)
			return ReadPacketAsync(session.Stream, ct);

		throw new InvalidOperationException("Session has no active transport.");
	}

	public static async Task<(byte flags, byte[] payload)?> ReadPacketAsync(NetworkStream stream, CancellationToken ct = default)
	{
		byte[] hdr = await ReadExactAsync(stream, 3, ct);
		if (hdr.Length == 0) return null;

		return await DecodePacket(hdr, len => ReadExactAsync(stream, len, ct));
	}

	public static async Task<(byte flags, byte[] payload)?> ReadPacketAsync(WebSocket socket, CancellationToken ct = default)
	{
		byte[] message = await ReadMessageAsync(socket, ct);
		if (message.Length == 0) return null;
		if (message.Length < 3) throw new InvalidOperationException("Packet too short");

		byte[] hdr = message[..3];
		return await DecodePacket(hdr, len =>
		{
			if (message.Length < 3 + len) throw new InvalidOperationException("Incomplete packet payload");
			return Task.FromResult(message.AsSpan(3, len).ToArray());
		});
	}

	private static async Task<(byte flags, byte[] payload)?> DecodePacket(byte[] hdr, Func<int, Task<byte[]>> readPayload)
	{
		byte flags = hdr[0];
		bool compressed = (flags & 0x20) != 0;

		ushort len = BinaryPrimitives.ReadUInt16BigEndian(hdr.AsSpan(1, 2));
		byte[] payload = await readPayload(len);

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

	private static async Task<byte[]> ReadMessageAsync(WebSocket socket, CancellationToken ct)
	{
		byte[] buffer = new byte[4096];
		using MemoryStream ms = new MemoryStream();
		while (true)
		{
			WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, ct);
			if (result.MessageType == WebSocketMessageType.Close) return [];
			if (result.MessageType != WebSocketMessageType.Binary) continue;
			ms.Write(buffer, 0, result.Count);
			if (result.EndOfMessage) break;
		}
		return ms.ToArray();
	}
}
