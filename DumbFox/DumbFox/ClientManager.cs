using DumbFox.Handlers;
using Common.Utils;
using Sfs2X.Entities.Data;
using Sfs2X.Util;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace DumbFox;

public static class ClientManager
{
    public static Task HandleClient(TcpClient client, CancellationToken ct = default) =>
        HandleClient(new Session(client), ct, () =>
        {
            client.Close();
            return Task.CompletedTask;
        });

    public static Task HandleClient(WebSocket socket, CancellationToken ct = default) =>
        HandleClient(new Session(socket), ct, async () =>
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server close", CancellationToken.None);
            }
            catch { /* ignore */ }
            socket.Dispose();
        });

    private static async Task HandleClient(Session session, CancellationToken ct, Func<Task> closeConnection)
    {
        string transport = session.Socket is not null ? "WebSocket" : "TCP";
        Console.WriteLine($"{transport} client connected");

        try
        {
            if (session.Client is { } client)
                client.NoDelay = true;

            while (!ct.IsCancellationRequested)
            {
                (byte flags, byte[] payload)? pkt = await SfsRead.ReadPacketAsync(session, ct);
                if (pkt == null)
                {
                    Console.WriteLine($"{transport} client disconnected (EOF)");
                    break;
                }

                (byte flags, byte[] payload) = pkt.Value;

                SFSObject outer;
                try
                {
                    outer = SFSObject.NewFromBinaryData(new ByteArray(payload));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Decode error: {ex.Message}");
                    break;
                }

                if (!outer.ContainsKey("c") || !outer.ContainsKey("a") || !outer.ContainsKey("p"))
                {
                    Console.WriteLine("Invalid packet: missing c/a/p");
                    break;
                }

                byte controller = outer.GetByte("c");
                short action = outer.GetShort("a");
                if (outer.GetSFSObject("p") is not SFSObject p)
                {
                    Console.WriteLine("Invalid p object type");
                    return;
                }

                Console.WriteLine($"[{transport}] controller={controller} action={action} keys=[{string.Join(",", p.GetKeys())}]");

                await RequestHandler.HandleRequest(action, controller, p, session);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"{transport} client manager canceled");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO error: {ex.Message}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error: {ex.Message}");
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled client error: {ex}");
        }
        finally
        {
            session.Cleanup();
            try { await closeConnection(); } catch { /* ignore */ }
            Console.WriteLine($"{transport} client closed");
        }
    }
}
