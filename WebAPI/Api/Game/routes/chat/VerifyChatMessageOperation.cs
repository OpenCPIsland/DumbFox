using Common.Utils;
using Common.Utils.Game;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAPI.Api.Game.routes.chat;

public class VerifyChatMessageOperation
{
    public static async Task Handle(HttpContext ctx)
    {
        Console.WriteLine("[WebAPI][GameRoutes][ChatApi] Got VerifyChatMessageOperation");
        var sw = Stopwatch.StartNew();

        ctx.Request.EnableBuffering();
        using StreamReader reader = new StreamReader(
            ctx.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);
        using var ms = new MemoryStream();
        await reader.BaseStream.CopyToAsync(ms);
        byte[] blob = ms.ToArray();

        ctx.Request.Body.Position = 0;

        ctx.Response.Headers["X-Cloud-Trace-Context"] = GameRoutes.Generate();
        ctx.Response.Headers["Server"] = "Google Frontend";

        ctx.Response.Headers["X-Encryption-Key-Id"] = "a----bad";
        object? result = new { Code = 999 };
        ctx.Response.ContentType = "application/json;charset=utf-8";
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        if (ctx.Request.Headers.TryGetValue("X-Encryption-Key-Id", out var XEncKeyidHeader))
        {
        }
        else
        {
            sw.Stop();
            ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { }, 401);
        }
        string decrypted = DumbfoxEncryption.DecryptIvPrependedBytes(blob, Convert.FromBase64String(ctx.Request.Headers["X-Encryption-Key-Id"].ToString()));
        Console.WriteLine($"[WebAPI][GameRoutes][ChatApi][From Client by VerifyChatMessageOperation] Decrypted body: {decrypted}");

        string accesstoken = "";
        if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            string[] words = authHeader.ToString().Split(",");
            var decodedBytes = Convert.FromBase64String(words[0].Replace("Basic ", ""));
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            accesstoken = decodedString.Replace(":", "");
        }
        else
        {
            sw.Stop();
            ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { }, 401);
        }

        var session = await DatabaseManager.GetSessionAccessToken(accesstoken);
        if (session is null)
        {
            sw.Stop();
            ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
            await ResponseHelper.WriteJsonAsync(ctx, new BaseCodeError { }, 401);
        } else
        {

            ChatMessage? obj = JsonSerializer.Deserialize<ChatMessage>(decrypted);

            result = new SignedResponse<ChatMessage>
            {
                data = new ChatMessage
                {
                    senderSessionId = (long)session["sessionId"],
                    message = obj.message,
                    emotion = obj.emotion,
                    moderated = false,
                    questId = obj.questId,
                    objective = obj.objective
                }
            };

            var json = JsonSerializer.Serialize(result, options);
            byte[] encrypted = DumbfoxEncryption.EncryptToIvPrependedBytes(json, Convert.FromBase64String(ctx.Request.Headers["X-Encryption-Key-Id"].ToString()));

            ctx.Response.ContentLength = encrypted.Length;

            sw.Stop();
            ctx.Response.Headers["X-Execution-Time"] = $"{sw.Elapsed.TotalMilliseconds:F0}";
            await ctx.Response.Body.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }
}

