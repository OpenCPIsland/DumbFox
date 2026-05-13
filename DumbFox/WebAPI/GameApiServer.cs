using Common.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using WebAPI.Api.Game;

namespace WebAPI;

public class GameApiServer
{
    public static async Task RunAsync(CancellationToken ct = default)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.WebHost.ConfigureKestrel(kestrelOptions =>
        {
            kestrelOptions.ListenAnyIP(5784);
        });

        Console.WriteLine($"DumbFox GAME API for Apache/Nginx on port {5784}");
        WebApplication app = builder.Build();

        app.Use(async (ctx, next) =>
        {
            DateTime start = DateTime.UtcNow;

            IPAddress? ip = ctx.Connection.RemoteIpAddress;
            string method = ctx.Request.Method;
            PathString path = ctx.Request.Path;

            Console.WriteLine($"[GAMEAPI] --> {ip} {method} {path}");

            await next();

            TimeSpan duration = DateTime.UtcNow - start;

            Console.WriteLine($"[GAMEAPI] <-- {ctx.Response.StatusCode} {method} {path} {duration.TotalMilliseconds:F0}ms");
        });

        app.Use(async (context, next) =>
        {
            string? requestPath = context.Request.Path.Value;

            await next();
        });

        GameRoutes.Register(app);

        await app.StartAsync(ct);
    }
}

