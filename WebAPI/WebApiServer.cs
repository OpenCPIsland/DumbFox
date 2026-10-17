using Common.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using MySqlConnector;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebAPI.Api.Game;
using WebAPI.Api.Jgc;
using WebAPI.Api.Mix;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebAPI;

public class WebApiServer
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

    public static async Task RunAsync(int port, int porthttps,  CancellationToken ct = default)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        await using var conn = await DatabaseManager.OpenConnectionAsync();
        // await using var cmd = new MySqlCommand("DELETE FROM sessions;",conn);
        //await cmd.ExecuteNonQueryAsync();
        await using var cmd0 = new MySqlCommand("DELETE FROM rooms;", conn);
        await cmd0.ExecuteNonQueryAsync();
        await using var cmd1 = new MySqlCommand("DELETE FROM room_users;", conn);
        await cmd1.ExecuteNonQueryAsync();

        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.WebHost.ConfigureKestrel(kestrelOptions =>
        {
            var cert = X509Certificate2.CreateFromPemFile("cert.pem", "key.pem");

            // Important: on Linux you often must re-import for compatibility
            cert = new X509Certificate2(cert.Export(X509ContentType.Pkcs12));

            kestrelOptions.ListenAnyIP(porthttps, listOptions =>
            {
                listOptions.UseHttps(cert);
                listOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });

            kestrelOptions.ListenAnyIP(port);
        });

        if (port == 80 && porthttps == 443)
        {
            Console.WriteLine($"DumbFox WebAPI: In your browser, open the URL, http://127.0.0.1");
            Console.WriteLine($"Club Penguin Island In-Game Setup Debug Menu > Network > Web Services");
            Console.WriteLine($"Account: http://127.0.0.1/mix");
            Console.WriteLine($"Game: http://127.0.0.1");
            Console.WriteLine($"Login: http://127.0.0.1");
            Console.WriteLine($"Login Part 2: http://127.0.0.1");

            Console.WriteLine($" ");

            Console.WriteLine($"And for https WARN: IT REQUIRES A VALID CERT AND KEY or domain with like CLOUDFLARE or with LetsEncrypt will not work with openssl(these remain still a not valid certificates)");
            Console.WriteLine($" ");
            Console.WriteLine($"DumbFox WEBAPI in browser you can open as https://127.0.0.1");
            Console.WriteLine($"Club Penguin Island In-Game Setup Debug Menu > Network > Web Services");
            Console.WriteLine($"Account: https://127.0.0.1/mix");
            Console.WriteLine($"Game: https://127.0.0.1");
            Console.WriteLine($"Login: https://127.0.0.1");
            Console.WriteLine($"Login Part 2: https://127.0.0.1");

            Console.WriteLine($" ");
        }
        else
        {
            Console.WriteLine($"DumbFox WebAPI: In your browser, open the URL, http://127.0.0.1:{port}");
            Console.WriteLine($"Club Penguin Island In-Game Setup Debug Menu > Network > Web Services");
            Console.WriteLine($"Account: http://127.0.0.1:{port}/mix");
            Console.WriteLine($"Game: http://127.0.0.1:{port}");
            Console.WriteLine($"Login: http://127.0.0.1:{port}");
            Console.WriteLine($"Login Part 2: http://127.0.0.1:{port}");
            Console.WriteLine($" ");
            if (porthttps == 443)
            {
                Console.WriteLine($"And for https WARN: IT REQUIRES A VALID CERT AND KEY or domain with like CLOUDFLARE or with LetsEncrypt will not work with openssl(these remain still a not valid certificates)");
                Console.WriteLine($" ");
                Console.WriteLine($"DumbFox WebAPI: In your browser, open the URL, https://127.0.0.1");
                Console.WriteLine($"Club Penguin Island In-Game Setup Debug Menu > Network > Web Services");
                Console.WriteLine($"Account: https://127.0.0.1/mix");
                Console.WriteLine($"Game: https://127.0.0.1");
                Console.WriteLine($"Login: https://127.0.0.1");
                Console.WriteLine($"Login Part 2: https://127.0.0.1");
                Console.WriteLine($" ");
            }
            else
            {
                Console.WriteLine($"And for https WARN: IT REQUIRES A VALID CERT AND KEY or domain with like CLOUDFLARE or with LetsEncrypt will not work with openssl(these remain still a not valid certificates)");
                Console.WriteLine($" ");
                Console.WriteLine($"DumbFox WebAPI: In your browser, open the URL, https://127.0.0.1:{porthttps}");
                Console.WriteLine($"Club Penguin Island In-Game Setup Debug Menu > Network > Web Services");
                Console.WriteLine($"Account: https://127.0.0.1:{porthttps}/mix");
                Console.WriteLine($"Game: https://127.0.0.1:{porthttps}");
                Console.WriteLine($"Login: https://127.0.0.1:{porthttps}");
                Console.WriteLine($"Login Part 2: https://127.0.0.1:{porthttps}");
                Console.WriteLine($" ");
            }
        }
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

            Console.WriteLine($"[WebAPI] --> {ip} {method} {path}");

            await next();

            TimeSpan duration = DateTime.UtcNow - start;

            Console.WriteLine($"[WebAPI] <-- {ctx.Response.StatusCode} {method} {path} {duration.TotalMilliseconds:F0}ms");
        });

        app.Use(async (context, next) =>
        {

            string? requestPath = context.Request.Path.Value;

            await next();
        });

        app.MapGet("/", () => Results.Text("Hello over HTTPS!"));
        var provider = new FileExtensionContentTypeProvider();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(builder.Environment.ContentRootPath, "www")),
            ContentTypeProvider = provider,
            RequestPath = ""
        });

        JgcRoutes.Register(app);
        MixRoutes.Register(app);
        GameRoutes.Register(app);

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

