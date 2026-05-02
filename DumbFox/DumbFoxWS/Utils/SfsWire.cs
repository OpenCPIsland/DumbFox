using System;
using System.Buffers.Binary;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Sfs2X.Entities.Data;
using Sfs2X.Util;

namespace DumbFoxWS;

public static class SfsWire
{
	private const byte FlagBinary = 0x80;

	public static async Task WriteMessageAsync(
		WebSocket socket,
		byte controller,
		short action,
		SFSObject parameters,
		CancellationToken ct = default)
	{

		var outer = new SFSObject();
		outer.PutByte("c", controller);
		outer.PutShort("a", action);
		outer.PutSFSObject("p", (SFSObject)parameters);

		await WritePacketAsync(socket, outer, ct);
	}

	private static async Task WritePacketAsync(WebSocket socket, ISFSObject obj, CancellationToken ct = default)
	{

		ByteArray ba = obj.ToBinary();

		// Common SmartFox ByteArray has ToArray(); if yours doesn’t, replace with ba.Bytes or ba.Buffer
		byte[] payload = ba.Bytes;
		int len = payload.Length;

		if (len > ushort.MaxValue)
			throw new InvalidOperationException($"Packet too large ({len} bytes). bigSized not implemented yet.");

		byte[] header = new byte[3];
		header[0] = FlagBinary;
		BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(1, 2), (ushort)len);

		byte[] packet = header.Concat(payload).ToArray();

		await socket.SendAsync(packet, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: ct);
	}
}
