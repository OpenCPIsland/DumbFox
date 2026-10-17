
using Common.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using WebAPI.Api.Jgc;

namespace WebAPI;

public static class JgcApiServer
{
    public static string MakeFakeUploadId()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(73);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static byte[] ComputeETag(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hash = md5.ComputeHash(stream);
        return hash;
    }

    static readonly uint[] Table = CreateTable();

    static uint[] CreateTable()
    {
        const uint poly = 0x82F63B78;
        var table = new uint[256];

        for (uint i = 0; i < table.Length; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
                crc = (crc & 1) != 0 ? poly ^ (crc >> 1) : crc >> 1;
            table[i] = crc;
        }

        return table;
    }

    public static string ComputeCrc32cBase64(string filePath)
    {
        using var stream = File.OpenRead(filePath);

        uint crc = 0xffffffff;

        int b;
        while ((b = stream.ReadByte()) != -1)
            crc = Table[(crc ^ (byte)b) & 0xff] ^ (crc >> 8);

        crc = ~crc;

        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, crc);

        return Convert.ToBase64String(bytes);
    }

    public static async Task RunAsync(int port, CancellationToken ct = default)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();

        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.WebHost.ConfigureKestrel(kestrelOptions =>
        {
            kestrelOptions.ListenAnyIP(port);
        });

        Console.WriteLine($"DumbFox JGC API for Apache/Nginx vhosts or proxypass on port {port}");
        Console.WriteLine($" ");
        WebApplication app = builder.Build();

        app.Use(async (ctx, next) =>
        {
            DateTime start = DateTime.UtcNow;
            IPAddress? ip = ctx.Connection.RemoteIpAddress;
            string method = ctx.Request.Method;
            PathString path = ctx.Request.Path;
            Console.WriteLine($"[JGCAPI] --> {ip} {method} {path}");
            await next();
            TimeSpan duration = DateTime.UtcNow - start;
            Console.WriteLine($"[JGCAPI] <-- {ctx.Response.StatusCode} {method} {path} {duration.TotalMilliseconds:F0}ms");
        });

        app.Use(async (context, next) =>
        {

            string? requestPath = context.Request.Path.Value;

            await next();
        });

        var provider = new FileExtensionContentTypeProvider();
        JgcRoutes.Register(app);

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "CDN")),
            ContentTypeProvider = provider,
            RequestPath = "",
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers["X-GUploader-UploadID"] = MakeFakeUploadId();
                ctx.Context.Response.Headers["x-goog-generation"] = "1769443505036321";
                ctx.Context.Response.Headers["ETag"] = $"\"{Convert.ToHexString(ComputeETag(ctx.File.PhysicalPath)).ToLowerInvariant()}\"";
                ctx.Context.Response.Headers["x-goog-metageneration"] = "1";
                ctx.Context.Response.Headers["x-goog-stored-content-encoding"] = "identity";
                ctx.Context.Response.Headers["x-goog-stored-content-length"] = ctx.File.Length.ToString();
                ctx.Context.Response.Headers["x-goog-hash"] = new StringValues(new[] { "crc32c=" + ComputeCrc32cBase64(ctx.File.PhysicalPath), "md5=" + Convert.ToBase64String(ComputeETag(ctx.File.PhysicalPath)) });
                ctx.Context.Response.Headers["x-goog-storage-class"] = "STANDARD";
                ctx.Context.Response.Headers["Server"] = "UploadServer";
                ctx.Context.Response.Headers["Vary"] = "Accept-Encoding";
                ctx.Context.Response.Headers["Cache-Control"] = "public, must-revalidate, proxy-revalidate, max-age=0";
                ctx.Context.Response.Headers["Expires"] = DateTime.UtcNow.ToString("R");

            }
        });
        
        await app.StartAsync(ct);
	}
}
