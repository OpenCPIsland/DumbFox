using Common.Utils;
using Common.Utils.Game;
using Common.Utils.Mix;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Api.Game.routes.encryption;

public class GetEncryptionKeyOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][GameRoutes][GameApi] Got GetEncryptionKeyOperation");
       
        ctx.Request.EnableBuffering();

        using StreamReader reader = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        ctx.Request.Body.Position = 0;

        Console.WriteLine($"[WebAPI][GameRoutes][GameApi][From Client by GetEncryptionKeyOperation] Body: {body}");
        var sw = Stopwatch.StartNew();

        ctx.Response.Headers["X-Cloud-Trace-Context"] = GameRoutes.Generate();
        ctx.Response.Headers["Server"] = "Google Frontend";
        ctx.Response.ContentType = "application/json;charset=utf-8";

        object? result = new { Code = 999 };
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        PublicKeyData? obj = JsonSerializer.Deserialize<PublicKeyData>(body);

        byte[] modulus = Convert.FromBase64String(obj.modulus);
        byte[] exponent = Convert.FromBase64String(obj.exponent);

        byte[] aesKey = DumbfoxEncryption.RandomAes256Key();

        byte[] encrypted;
        using (RSA rsa = DumbfoxEncryption.BuildRsaFromModExp(modulus, exponent))
        {
            encrypted = DumbfoxEncryption.RsaEncryptPkcs1(rsa, aesKey);
        }

        string ekB64 = Convert.ToBase64String(encrypted);

        result = new EncryptionKey
        {
            keyId = Convert.ToBase64String(aesKey),
            encryptedSymmetricKey = ekB64
        };

        var json = JsonSerializer.Serialize(result, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        sw.Stop();
        ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
        ctx.Response.ContentLength = bytes.Length;

        await ctx.Response.WriteAsync(json);

    }
}

