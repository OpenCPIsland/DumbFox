using Common.Utils;
using DumbFox.Handlers;
using Microsoft.AspNetCore.Http;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;
using Sfs2X.Util;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace DumbFox;

public static class ClientManager
{
    // Same heartbeat policy used by the TCP path, now shared by TCP and WebSocket.
    // The SmartFox client sends an application-level PingPong request and expects
    // the server to answer it. RequestHandler.HandlePingPong updates LastPongUtc.
    private const int HEARTBEAT_CHECK_INTERVAL_SECONDS = 5;
    private const int HEARTBEAT_TIMEOUT_SECONDS = 30;

    private static async Task RunHeartbeatSupervisor(Session session, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(HEARTBEAT_CHECK_INTERVAL_SECONDS),
                    ct);

                if (!session.IsLoggedIn)
                    continue;

                if (DateTime.UtcNow - session.LastPongUtc <=
                    TimeSpan.FromSeconds(HEARTBEAT_TIMEOUT_SECONDS))
                    continue;

                session.IsDisconnected = true;
                Console.WriteLine(
                    $"Heartbeat timeout: {session.UserName} ({session.UserId}) " +
                    $"has not sent a PingPong for {HEARTBEAT_TIMEOUT_SECONDS} seconds. " +
                    "Closing session.");
                return;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Normal shutdown of the heartbeat task.
        }
    }

    public static async Task<Task> HandlePacket(UdpClient server, IPEndPoint remote, byte[] buffer, CancellationToken ct)
    {
        string message = Encoding.UTF8.GetString(buffer);

        Console.WriteLine($"UDP Received from {remote}: {message}");

        await server.SendAsync(buffer, remote, ct);

        return Task.CompletedTask;
    }

    public static Task HandleClient(TcpClient client, CancellationToken ct = default)
    {
        client.Client.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive,
            true);

        var session = new Session(client)
        {
            RemoteEndPoint = (IPEndPoint?)client.Client.RemoteEndPoint,
            LocalEndPoint = (IPEndPoint?)client.Client.LocalEndPoint
        };

        return HandleClient(session, ct, () =>
        {
            try { session.Stream?.Close(); } catch { }
            try { session.Stream?.Dispose(); } catch { }
            try { session.Client?.Close(); } catch { }
            try { session.Client?.Dispose(); } catch { }
            return Task.CompletedTask;
        });
    }

    public static Task HandleClient(Session session, CancellationToken ct = default) =>
        HandleClient(session, ct, async () =>
        {
            if (session.Socket is { } ws)
            {
                try
                {
                    if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    {
                        using CancellationTokenSource closeCts =
                            new CancellationTokenSource(TimeSpan.FromSeconds(2));

                        await ws.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Server close",
                            closeCts.Token);
                    }
                }
                catch { }

                try { ws.Abort(); } catch { }
                try { ws.Dispose(); } catch { }
            }
        });

    private static async Task HandleClient(
        Session session,
        CancellationToken ct,
        Func<Task> closeConnection)
    {
        string transport = session.Socket is not null ? "WebSocket" : "TCP";
        Console.WriteLine($"{transport} client connected");

        using CancellationTokenSource cts =
            CancellationTokenSource.CreateLinkedTokenSource(ct);

        Task readTask = RunReadLoop(session, cts.Token);
        Task heartbeatTask = RunHeartbeatSupervisor(session, cts.Token);

        try
        {
            if (session.Client is { } client)
                client.NoDelay = true;

            Task completed = await Task.WhenAny(readTask, heartbeatTask);

            // If heartbeat wins, the read loop may still be blocked in a socket
            // ReceiveAsync/ReadAsync. Cancelling the shared token forces it out.
            if (completed == heartbeatTask && session.IsDisconnected)
            {
                Console.WriteLine(
                    $"{transport} heartbeat timeout -> aborting session " +
                    $"(userId={session.UserId}, user='{session.UserName}').");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Console.WriteLine($"{transport} client manager canceled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{transport} client manager error: {ex}");
        }
        finally
        {
            cts.Cancel();

            // Give the read loop a short chance to observe cancellation. Do not
            // let a broken network transport keep the session alive indefinitely.
            try
            {
                await readTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch { }

            try
            {
                await heartbeatTask.WaitAsync(TimeSpan.FromSeconds(1));
            }
            catch { }

            session.Cleanup();

            try { await closeConnection(); } catch { }

            Console.WriteLine($"{transport} client closed");
        }
    }

    private static async Task RunReadLoop(Session session, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            (byte flags, byte[] payload)? pkt =
                await SfsRead.ReadPacketAsync(session, ct);

            if (pkt == null)
            {
                Console.WriteLine("SFS transport EOF/close received.");
                return;
            }

            (_, byte[] packetPayload) = pkt.Value;

            SFSObject outer;
            try
            {
                outer = SFSObject.NewFromBinaryData(new ByteArray(packetPayload));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decode error: {ex.Message}");
                return;
            }

            if (!outer.ContainsKey("c") ||
                !outer.ContainsKey("a") ||
                !outer.ContainsKey("p"))
            {
                Console.WriteLine("Invalid packet: missing c/a/p");
                return;
            }

            byte controller = outer.GetByte("c");
            short action = outer.GetShort("a");

            if (outer.GetSFSObject("p") is not SFSObject p)
            {
                Console.WriteLine("Invalid p object type");
                return;
            }

            Console.WriteLine(
                $"[{(session.Socket is not null ? "WebSocket" : "TCP")}] " +
                $"controller={controller} action={action} " +
                $"keys=[{string.Join(",", p.GetKeys())}]");

            // RequestHandler.HandlePingPong updates LastPongUtc and sends the
            // SmartFox PingPong response. Keeping it in RequestHandler means
            // TCP and WebSocket use exactly the same application heartbeat.
            await RequestHandler.HandleRequest(action, controller, p, session);
        }
    }
}
