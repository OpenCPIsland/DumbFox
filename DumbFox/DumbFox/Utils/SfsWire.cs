using Common.Utils;
using DumbFox.Utils;
using Sfs2X.Entities.Data;
using Sfs2X.Util;
using Sfs2X.WebSocketSharp;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace DumbFox;

public static class SfsWire
{
	private const byte FlagBinary = 0x80;
	private const byte BigSized = 8;


    public static Task WriteMessageAsync(
		Session session,
		byte controller,
		short action,
		SFSObject parameters,
		CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(parameters);

		var outer = new SFSObject();
		outer.PutByte("c", controller);
		outer.PutShort("a", action);
		outer.PutSFSObject("p", (SFSObject)parameters);

		return WritePacketAsync(session, outer, ct);
	}

    private static readonly SemaphoreSlim _writeLock = new(1, 1);

    
    private static async Task WritePacketAsync(Session session, ISFSObject obj, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(obj);

		ByteArray ba = obj.ToBinary();
		byte[] payload = ba.Bytes;
		int len = payload.Length;


       // if (len > ushort.MaxValue)
		//	throw new InvalidOperationException($"Packet too large ({len} bytes). bigSized not implemented yet.");

		/*byte[] header = new byte[3];
		header[0] = FlagBinary;
		BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(1, 2), (ushort)len);

		byte[] packet = header.Concat(payload).ToArray();*/

        byte[] header;
        byte[] packet;

        if (len <= ushort.MaxValue)
        {
            header = new byte[3];
            header[0] = FlagBinary;
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(1, 2), (ushort)len);
        }
        else
        {

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Packet too large ({len} bytes). bigSized not implemented yet.");
            Console.ResetColor();
            Console.WriteLine(" ");
            header = new byte[5];
            header[0] = BigSized; // replace with actual big-sized flag when known
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(1, 4), (uint)len);
        }

        packet = header.Concat(payload).ToArray();

        if (session.Socket is not null)
		{
			await session.Socket.SendAsync(packet, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: ct);
			return;
		}

		if (session.Stream is not null)
		{
            await session.Stream.WriteAsync(packet, ct);
            return;
		}

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Session has no active transport.");
        Console.ResetColor();
    }
}
