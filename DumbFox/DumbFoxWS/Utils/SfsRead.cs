using DumbFoxWS.Utils;
namespace DumbFoxWS;

using System.Buffers.Binary;
using System.Net.WebSockets;

public static class SfsRead
{
	public static async Task<(byte flags, byte[] payload)?> ReadPacketAsync(WebSocket socket, CancellationToken ct = default)
	{
		byte[] message = await ReadMessageAsync(socket, ct);
		if (message.Length == 0) return null;
		if (message.Length < 3) throw new InvalidOperationException("Packet too short");

		byte[] hdr = message[..3];

		byte flags = hdr[0];
		bool compressed = (flags & 0x20) != 0;

		// small size variant: 2-byte length
		ushort len = BinaryPrimitives.ReadUInt16BigEndian(hdr.AsSpan(1, 2));
		if (message.Length < 3 + len) throw new InvalidOperationException("Incomplete packet payload");
		byte[] payload = message.AsSpan(3, len).ToArray();
		
		if (compressed)
			payload = Compression.ZlibDecompress(payload);

		return (flags, payload);
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