using Common.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using WebAPI.Api.Mix;

namespace WebAPI;

public class MixApiServer
{
    public static async Task RunAsync(CancellationToken ct = default)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.WebHost.ConfigureKestrel(kestrelOptions =>
        {
            kestrelOptions.ListenAnyIP(5783);
        });

        Console.WriteLine($"DumbFox MIX API for Apache/Nginx on port {5783}");
        WebApplication app = builder.Build();

        app.Use(async (ctx, next) =>
        {
            DateTime start = DateTime.UtcNow;
            IPAddress? ip = ctx.Connection.RemoteIpAddress;
            string method = ctx.Request.Method;
            PathString path = ctx.Request.Path;

            /*if (path.StartsWithSegments("/mix/notifications/sinceSequence") || path.StartsWithSegments("/notifications/sinceSequence"))
            {
                await next();
                return;
            }*/

            Console.WriteLine($"[MIXAPI] --> {ip} {method} {path}");
            await next();
            TimeSpan duration = DateTime.UtcNow - start;
            Console.WriteLine($"[MIXAPI] <-- {ctx.Response.StatusCode} {method} {path} {duration.TotalMilliseconds:F0}ms");
        });

        app.Use(async (context, next) =>
        {
            string? requestPath = context.Request.Path.Value;
            await next();
        });

        MixRoutes.Register(app);
        await app.StartAsync(ct);
    }
}

