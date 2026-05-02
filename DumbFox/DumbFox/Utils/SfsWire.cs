using System;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Sfs2X.Entities.Data;
using Sfs2X.Util;

namespace DumbFox;

public static class SfsWire
{
	private const byte FlagBinary = 0x80;

	public static async Task WriteMessageAsync(
		NetworkStream stream,
		byte controller,
		short action,
		SFSObject parameters,
		CancellationToken ct = default)
	{
		if (stream is null) throw new ArgumentNullException(nameof(stream));
		if (parameters is null) throw new ArgumentNullException(nameof(parameters));

		var outer = new SFSObject();
		outer.PutByte("c", controller);
		outer.PutShort("a", action);
		outer.PutSFSObject("p", (SFSObject)parameters);

		await WritePacketAsync(stream, outer, ct);
	}

	private static async Task WritePacketAsync(NetworkStream stream, ISFSObject obj, CancellationToken ct = default)
	{
		if (stream is null) throw new ArgumentNullException(nameof(stream));
		if (obj is null) throw new ArgumentNullException(nameof(obj));

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

		await stream.WriteAsync(packet, ct);
		//Old method of multiple writes
		//await stream.WriteAsync(header, 0, header.Length, ct);
		//await stream.WriteAsync(payload, 0, len, ct);
	}
}
