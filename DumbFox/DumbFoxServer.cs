using Common.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DumbFox.Handlers;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace DumbFox;

public static class DumbFoxServer
{
    private const string DefaultCertPath = "/etc/letsencrypt/live/opencpi.pannun.de/fullchain.pem";
    private const string DefaultKeyPath = "/etc/letsencrypt/live/opencpi.pannun.de/privkey.pem";

    public static async Task RunAsync(int port, CancellationToken ct = default)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"DumbFox SFS2x API TCP Server started on port {port}");
        Console.WriteLine($" ");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(ct);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                _ = ClientManager.HandleClient(client, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        finally
        {
            listener.Stop();
            Console.WriteLine("DumbFox TCP server stopped");
        }
    }

    public static async Task RunUdpAsync(int port, CancellationToken ct = default)
    {
        using UdpClient listener = new(port);

        Console.WriteLine($"DumbFox SFS2x API UDP Server started on port {port}");
        Console.WriteLine();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                UdpReceiveResult result = await listener.ReceiveAsync(ct);
                _ = ClientManager.HandlePacket(listener, result.RemoteEndPoint, result.Buffer, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    public static async Task RunWebSocketAsync(int wsPort, int wssPort, CancellationToken ct = default)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(DumbFoxServer).Assembly.FullName,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        X509Certificate2? tlsCert = TryLoadCertificate(DefaultCertPath, DefaultKeyPath);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(wsPort);

            if (wssPort > 0 && tlsCert is not null)
            {
                options.ListenAnyIP(wssPort, listenOptions => listenOptions.UseHttps(tlsCert));
            }
        });

        WebApplication app = builder.Build();
        app.UseWebSockets();

        app.Run(async context =>
        {
            if (HttpMethods.IsPost(context.Request.Method) &&
                string.Equals(context.Request.Path.Value, "/admin/restart-notice", StringComparison.OrdinalIgnoreCase))
            {
                IPAddress? remoteAddress = context.Connection.RemoteIpAddress;
                if (remoteAddress is null || !IPAddress.IsLoopback(remoteAddress))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }

                await ExtensionHandler.BroadcastServerRestartMessageAsync();
                await context.Response.WriteAsJsonAsync(new { sent = true, message = "Restart notice broadcast to connected players." });
                return;
            }

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("WebSocket request required.");
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var session = new Session(socket)
            {
                RemoteEndPoint = new IPEndPoint(context.Connection.RemoteIpAddress!, context.Connection.RemotePort),
                LocalEndPoint = new IPEndPoint(context.Connection.LocalIpAddress!, context.Connection.LocalPort)
            };

            await ClientManager.HandleClient(session, ct);
        });

        await app.StartAsync(ct);

        Console.WriteLine($"DumbFox WebSocket Sfs2x API server started on ws://0.0.0.0:{wsPort}");
        if (wssPort > 0 && tlsCert is not null)
            Console.WriteLine($"DumbFox WebSocket Secure Sfs2x API server started on wss://0.0.0.0:{wssPort}");
        else if (wssPort > 0)
            Console.WriteLine($"DumbFox WSS disabled (no TLS certificate found at {DefaultCertPath})");
        Console.WriteLine($" ");

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        finally
        {
            await app.StopAsync(CancellationToken.None);
            Console.WriteLine("DumbFox WebSocket server stopped");
        }
    }

    private static X509Certificate2? TryLoadCertificate(string certPath, string keyPath)
    {
        try
        {
            if (!File.Exists(certPath) || !File.Exists(keyPath))
                return null;

            return X509Certificate2.CreateFromPemFile(certPath, keyPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DumbFox WSS certificate load failed: {ex.Message}");
            return null;
        }
    }
}